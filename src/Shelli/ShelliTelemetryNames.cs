namespace HeyShelli
{
    /// <summary>
    /// Stable public names for every telemetry point Shelli emits.
    /// These names are a public contract consumed by collectors and dashboards; they do not change within a major version.
    /// Shelli emits through <see cref="System.Diagnostics.Metrics.Meter"/> and <see cref="System.Diagnostics.ActivitySource"/> only,
    /// takes no exporter dependency, and costs effectively nothing until a listener subscribes to <see cref="MeterName"/> or <see cref="ActivitySourceName"/>.
    /// </summary>
    public static class ShelliTelemetryNames
    {
        #region Sources

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> that carries every Shelli metric.
        /// </summary>
        public const string MeterName = "Shelli";

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.ActivitySource"/> that carries every Shelli span.
        /// </summary>
        public const string ActivitySourceName = "Shelli";

        #endregion

        #region Metrics

        /// <summary>
        /// Histogram (seconds): end-to-end duration of one command execution, from process creation through exit.
        /// Labels: process.executable.name, shelli.outcome, error.type (only when shelli.outcome is 'error').
        /// </summary>
        public const string CommandDuration = "shelli.command.duration";

        /// <summary>
        /// Counter ({command}): completed command executions.
        /// Labels: process.executable.name, shelli.outcome, error.type (only when shelli.outcome is 'error').
        /// </summary>
        public const string CommandExecutions = "shelli.command.executions";

        /// <summary>
        /// UpDownCounter ({command}): commands currently executing (in-flight subprocesses).
        /// Labels: process.executable.name.
        /// </summary>
        public const string CommandActive = "shelli.command.active";

        /// <summary>
        /// Histogram (seconds): duration of each execution stage ('start' is process creation, 'wait' is runtime until exit).
        /// Labels: process.executable.name, shelli.stage, shelli.outcome ('success' or 'error').
        /// </summary>
        public const string StageDuration = "shelli.command.stage.duration";

        /// <summary>
        /// Counter ({line}): lines read from the child process output streams.
        /// Labels: process.executable.name, shelli.stream ('stdout' or 'stderr').
        /// </summary>
        public const string OutputLines = "shelli.command.output.lines";

        /// <summary>
        /// Observable gauge ({info}): constant 1, labeled with the Shelli library version, for build-info joins.
        /// Labels: shelli.version.
        /// </summary>
        public const string LibraryInfo = "shelli.library.info";

        #endregion

        #region Spans

        /// <summary>
        /// Span name for one command execution.
        /// </summary>
        public const string ExecSpan = "shelli exec";

        /// <summary>
        /// Span name for the process creation stage.
        /// </summary>
        public const string StageStartSpan = "stage:start";

        /// <summary>
        /// Span name for the stage that waits for the process to exit.
        /// </summary>
        public const string StageWaitSpan = "stage:wait";

        #endregion

        #region Attributes

        /// <summary>
        /// Attribute: file name of the shell executable (for example 'sh', 'bash', 'cmd.exe'). Never the full command line.
        /// </summary>
        public const string AttributeExecutableName = "process.executable.name";

        /// <summary>
        /// Attribute: execution outcome. One of <see cref="OutcomeSuccess"/>, <see cref="OutcomeNonZeroExit"/>, <see cref="OutcomeError"/>.
        /// </summary>
        public const string AttributeOutcome = "shelli.outcome";

        /// <summary>
        /// Attribute: full type name of the exception that caused an 'error' outcome.
        /// </summary>
        public const string AttributeErrorType = "error.type";

        /// <summary>
        /// Attribute: stage name. One of <see cref="StageStart"/>, <see cref="StageWait"/>.
        /// </summary>
        public const string AttributeStage = "shelli.stage";

        /// <summary>
        /// Attribute: output stream. One of <see cref="StreamStdout"/>, <see cref="StreamStderr"/>.
        /// </summary>
        public const string AttributeStream = "shelli.stream";

        /// <summary>
        /// Attribute: Shelli library version.
        /// </summary>
        public const string AttributeVersion = "shelli.version";

        /// <summary>
        /// Span attribute: child process id.
        /// </summary>
        public const string AttributePid = "process.pid";

        /// <summary>
        /// Span attribute: child process exit code.
        /// </summary>
        public const string AttributeExitCode = "process.exit.code";

        /// <summary>
        /// Span attribute: length in characters of the command text. The command text itself is never recorded because it may contain secrets.
        /// </summary>
        public const string AttributeCommandLength = "shelli.command.length";

        /// <summary>
        /// Span attribute: whether W3C trace context was handed to the child process through the TRACEPARENT environment variable.
        /// </summary>
        public const string AttributeContextPropagated = "shelli.trace_context.propagated";

        /// <summary>
        /// Span attribute: number of stdout lines read.
        /// </summary>
        public const string AttributeStdoutLines = "shelli.stdout.lines";

        /// <summary>
        /// Span attribute: number of stderr lines read.
        /// </summary>
        public const string AttributeStderrLines = "shelli.stderr.lines";

        #endregion

        #region Values

        /// <summary>
        /// Outcome: the process exited with code 0.
        /// </summary>
        public const string OutcomeSuccess = "success";

        /// <summary>
        /// Outcome: the process ran and exited with a non-zero code.
        /// </summary>
        public const string OutcomeNonZeroExit = "nonzero_exit";

        /// <summary>
        /// Outcome: an exception was thrown (for example the shell executable could not be started).
        /// </summary>
        public const string OutcomeError = "error";

        /// <summary>
        /// Stage: process creation.
        /// </summary>
        public const string StageStart = "start";

        /// <summary>
        /// Stage: waiting for the process to exit.
        /// </summary>
        public const string StageWait = "wait";

        /// <summary>
        /// Stream: standard output.
        /// </summary>
        public const string StreamStdout = "stdout";

        /// <summary>
        /// Stream: standard error.
        /// </summary>
        public const string StreamStderr = "stderr";

        /// <summary>
        /// Environment variable that carries the W3C traceparent to the child process.
        /// </summary>
        public const string TraceParentEnvironmentVariable = "TRACEPARENT";

        /// <summary>
        /// Environment variable that carries the W3C tracestate to the child process.
        /// </summary>
        public const string TraceStateEnvironmentVariable = "TRACESTATE";

        #endregion
    }
}
