# Change Log

## Current Version

v2.2.0

- Built-in telemetry: `Shelli` meter and activity source (BCL only, no exporter dependency, near-zero cost when unobserved)
- Metrics: `shelli.command.duration`, `shelli.command.executions`, `shelli.command.active`, `shelli.command.stage.duration`, `shelli.command.output.lines`, `shelli.library.info`
- Spans: `shelli exec` with `stage:start` and `stage:wait` children, explicit status, exception events
- W3C trace context passed to child processes via `TRACEPARENT` / `TRACESTATE` (`PropagateTraceContext`, default `true`)
- Public `ShelliTelemetryNames` constants class
- The child `Process` object is now disposed after each `Go` call
- See `TELEMETRY.md`

## Previous Versions

v2.1.0

- Dependency updates, stream-isolation tests

v2.0.x

- Migrate from static class

v1.1.x

- Added `WindowsShell` (default: `cmd.exe`) and `LinuxShell` (default: `sh`) properties

v1.0.0

- Initial release
