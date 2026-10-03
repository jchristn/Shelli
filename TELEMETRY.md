# Shelli Telemetry

Shelli emits OpenTelemetry-compatible metrics and traces for every command it runs. It uses only the .NET BCL APIs (`System.Diagnostics.Metrics.Meter` and `System.Diagnostics.ActivitySource`). It has no exporter or SDK dependency and never opens a connection. Your host collects the data and sends it to Prometheus, Tempo, Grafana, or any OTLP backend.

Until a listener subscribes, the cost is close to zero: no span is created, no handlers are attached for line counting, and every instrument call returns early.

Shelli is a library, so it ships no `compose.yaml`, Grafana stack, or dashboards. Those belong to the host service. The PromQL below is ready to drop into a host's dashboards and alert rules.

## Names

| Kind | Name |
| --- | --- |
| Meter | `Shelli` |
| ActivitySource | `Shelli` |

All names (instruments, spans, attributes, and values) are public constants on `HeyShelli.ShelliTelemetryNames`. They are a stable contract within a major version.

## Subscribing

### Radiant

```csharp
RadiantSettings settings = new RadiantSettings("my-service");
settings.Sources.AddMeter(ShelliTelemetryNames.MeterName);           // "Shelli"
settings.Sources.AddActivitySource(ShelliTelemetryNames.ActivitySourceName); // "Shelli"
using (RadiantHost host = RadiantHost.Start(settings)) { /* run the app */ }
```

### OpenTelemetry .NET SDK

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("Shelli").AddPrometheusExporter())
    .WithTracing(t => t.AddSource("Shelli").AddOtlpExporter());
```

### Raw listeners

`MeterListener` (enable instruments whose `Meter.Name == "Shelli"`) and `ActivityListener` (`ShouldListenTo = s => s.Name == "Shelli"`) work as well. The test suite in `src/Test.Shared/TelemetryCapture.cs` is a complete example.

## Configuration

| Setting | Default | Effect |
| --- | --- | --- |
| `Shelli.PropagateTraceContext` | `true` | When a trace is active, sets `TRACEPARENT` (and `TRACESTATE` if present) in the child process environment, following the OpenTelemetry environment-variable carrier convention, so OpenTelemetry-aware children join the same trace. Set to `false` to leave the child environment untouched. |

There are no other knobs. Whether telemetry is collected is decided entirely by whether the host subscribes.

## Metrics

Prometheus names assume the standard OTLP or Prometheus exporter conversion (dots to underscores, unit suffix, `_total` on counters).

| Instrument | Prometheus family | Type | Unit | Labels | Description |
| --- | --- | --- | --- | --- | --- |
| `shelli.command.duration` | `shelli_command_duration_seconds` | Histogram | `s` | `process_executable_name`, `shelli_outcome`, `error_type`* | End-to-end duration of one `Go` call, from process creation through exit. |
| `shelli.command.executions` | `shelli_command_executions_total` | Counter | `{command}` | `process_executable_name`, `shelli_outcome`, `error_type`* | Completed executions by outcome. |
| `shelli.command.active` | `shelli_command_active` | UpDownCounter | `{command}` | `process_executable_name` | Commands currently running (in-flight child processes). |
| `shelli.command.stage.duration` | `shelli_command_stage_duration_seconds` | Histogram | `s` | `process_executable_name`, `shelli_stage`, `shelli_outcome` | Per-stage duration. `start` is process creation (fork/exec); `wait` is the runtime until exit. The histogram `_count` is the per-stage counter. |
| `shelli.command.output.lines` | `shelli_command_output_lines_total` | Counter | `{line}` | `process_executable_name`, `shelli_stream` | Lines read from the child's stdout or stderr. |
| `shelli.library.info` | `shelli_library_info` | Observable gauge | `{info}` | `shelli_version` | Always 1. Use it for build-info joins. |

\* `error.type` is present only when `shelli.outcome` is `error`.

### Label values (all bounded)

| Label | Values |
| --- | --- |
| `process.executable.name` | File name of the configured shell (`sh`, `bash`, `cmd.exe`, `powershell.exe`, ...). Set by the developer, never by end-user input. |
| `shelli.outcome` | `success` (exit code 0), `nonzero_exit` (ran, exit code not 0), `error` (an exception was thrown, for example the shell could not be started). For stages: `success` or `error`. |
| `error.type` | Full exception type name, for example `System.ComponentModel.Win32Exception`. |
| `shelli.stage` | `start`, `wait` |
| `shelli.stream` | `stdout`, `stderr` |

Exit codes, process ids, and command text are never metric labels. Exit code and pid are on the span.

### Histogram buckets

Shelli does not set bucket advice, so your collector's defaults apply. Shell commands range from milliseconds to many minutes. If your workload has long-running commands, configure a view for `shelli.command.duration` and `shelli.command.stage.duration`, for example `0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 120, 300, 600, 1800`.

## Spans

| Span | Kind | Parent | Attributes | Status |
| --- | --- | --- | --- | --- |
| `shelli exec` | Internal | `Activity.Current` at the time of `Go` (for example Watson's request span) | `process.executable.name`, `shelli.command.length`, `shelli.trace_context.propagated`, `process.pid`, `process.exit.code`, `shelli.outcome`, `shelli.stdout.lines`, `shelli.stderr.lines`, `error.type` (on exception) | `Ok` on exit code 0. `Error` with description `Process exited with code N` on non-zero exit. `Error` with an `exception` event (`exception.type`, `exception.message`, `exception.stacktrace`) when an exception is thrown. |
| `stage:start` | Internal | `shelli exec` | `shelli.stage` = `start` | `Ok`, or `Error` with an `exception` event when the shell cannot be started. |
| `stage:wait` | Internal | `shelli exec` | `shelli.stage` = `wait` | `Ok`, or `Error` with an `exception` event. |

The command text is never recorded on spans or metrics because it can contain secrets. Only its length (`shelli.command.length`) is recorded. Command output is never recorded; only line counts are.

### Trace propagation

- **Inbound:** `shelli exec` nests under whatever `Activity.Current` is when `Go` is called, so a command run inside a Watson route or a background job appears in that trace.
- **Outbound:** with `PropagateTraceContext` on, the child receives `TRACEPARENT=<exec span id>`. A child that reads it (any OpenTelemetry SDK honoring environment-variable propagation, or a script that forwards it as an HTTP `traceparent` header) continues the same trace. If Shelli's source is not subscribed but the host has an active trace, the host's current span is propagated instead.

## Reading the signals

- **Where did the time go?** In a trace, compare `stage:start` with `stage:wait`. A slow `start` points to the host (process creation, a missing or slow shell binary, resource pressure). A slow `wait` is the command itself.
- **What failed?** `shelli.outcome="error"` means Shelli could not run the shell; check `error.type` and the span's exception event. `shelli.outcome="nonzero_exit"` means the command ran and reported failure; `process.exit.code` on the span says which.
- **Is something stuck?** `shelli_command_active` staying high while `shelli_command_executions_total` stops growing means commands are hanging in `wait`.

## PromQL

```promql
# Command throughput by outcome
sum by (shelli_outcome) (rate(shelli_command_executions_total[5m]))

# Failure ratio (non-zero exits plus errors)
sum(rate(shelli_command_executions_total{shelli_outcome!="success"}[5m]))
  / sum(rate(shelli_command_executions_total[5m]))

# p95 command duration by shell
histogram_quantile(0.95, sum by (le, process_executable_name) (rate(shelli_command_duration_seconds_bucket[5m])))

# p95 per stage
histogram_quantile(0.95, sum by (le, shelli_stage) (rate(shelli_command_stage_duration_seconds_bucket[5m])))

# In-flight commands
sum by (process_executable_name) (shelli_command_active)

# Output volume
sum by (shelli_stream) (rate(shelli_command_output_lines_total[5m]))
```

### Recommended alerts

```yaml
groups:
  - name: shelli
    rules:
      - alert: ShelliShellCannotStart
        expr: sum by (error_type) (increase(shelli_command_executions_total{shelli_outcome="error"}[5m])) > 0
        for: 0m
        labels: { severity: critical }
        annotations:
          summary: "Shelli could not start the shell ({{ $labels.error_type }})"

      - alert: ShelliHighFailureRatio
        expr: |
          sum(rate(shelli_command_executions_total{shelli_outcome!="success"}[10m]))
            / sum(rate(shelli_command_executions_total[10m])) > 0.2
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "More than 20% of shell commands are failing"

      - alert: ShelliCommandsStuck
        expr: sum(shelli_command_active) > 0 and sum(increase(shelli_command_executions_total[15m])) == 0
        for: 15m
        labels: { severity: warning }
        annotations:
          summary: "Shell commands are in flight but none have completed in 15 minutes"

      - alert: ShelliSlowProcessStart
        expr: histogram_quantile(0.95, sum by (le) (rate(shelli_command_stage_duration_seconds_bucket{shelli_stage="start"}[10m]))) > 1
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "p95 process creation time is over 1s"
```

Tune the thresholds to your workload. Some commands (for example `grep` with no match) exit non-zero by design.

## Dashboards

Shelli ships no dashboards. In a host service's Grafana folder, Shelli panels belong on the Integrations (or Subprocesses) dashboard: throughput by outcome, failure ratio, p95 by stage, in-flight commands, and output lines by stream, using the queries above.
