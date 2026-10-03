namespace HeyShelli
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Shell runner class.
    /// Each call to <see cref="Go(string)"/> emits a span on the <see cref="ShelliTelemetryNames.ActivitySourceName"/> activity source
    /// and metrics on the <see cref="ShelliTelemetryNames.MeterName"/> meter; see TELEMETRY.md.
    /// Instances are not thread-safe for concurrent configuration changes; concurrent calls to Go on separate instances are safe.
    /// </summary>
    public class Shelli : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Action to invoke when data is received.
        /// </summary>
        public Action<string> OutputDataReceived = null;

        /// <summary>
        /// Action to invoke when error data is received.
        /// </summary>
        public Action<string> ErrorDataReceived = null;

        /// <summary>
        /// Windows shell command.  Defaults to 'cmd.exe'.  For certain commands and environments, it may be necessary to change this value.
        /// </summary>
        public string WindowsShell
        {
            get
            {
                return _WindowsShell;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(WindowsShell));
                _WindowsShell = value;
            }
        }

        /// <summary>
        /// Linux shell command.  Defaults to 'sh'.  For certain commands and environments, you may need to change this.  'bash' is a common alternative.
        /// </summary>
        public string LinuxShell
        {
            get
            {
                return _LinuxShell;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(LinuxShell));
                _LinuxShell = value;
            }
        }

        /// <summary>
        /// When true (default), and a trace is active, the W3C trace context of the execution span is passed to the child process
        /// through the TRACEPARENT and TRACESTATE environment variables so OpenTelemetry-aware children join the same trace.
        /// When false, the child environment is left untouched.
        /// </summary>
        public bool PropagateTraceContext { get; set; } = true;

        #endregion

        #region Private-Members

        private string _WindowsShell = "cmd.exe";
        private string _LinuxShell = "sh";
        private bool _Disposed = false;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a command and block until it exits.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>The exit code of the shell process.</returns>
        /// <exception cref="ArgumentNullException">Thrown when command is null or empty.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the configured shell cannot be started.</exception>
        public int Go(string command)
        {
            if (String.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));
            
            string filename = null;
            string args = null;

            // filename  i.e. "cmd.exe"
            // args      i.e. "/c dir /w"

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filename = WindowsShell;
                args = "/c \"" + command + "\"";
            }
            else
            {
                filename = LinuxShell;
                args = "-c \"" + command + "\"";
            }

            string executableName = GetExecutableName(filename);
            long startTimestamp = Stopwatch.GetTimestamp();
            string outcome = ShelliTelemetryNames.OutcomeError;
            string errorType = null;
            long stdoutLines = 0;
            long stderrLines = 0;
            bool countLines = false;

            Activity activity = ShelliTelemetry.StartActivity(ShelliTelemetryNames.ExecSpan, ActivityKind.Internal);
            ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeExecutableName, executableName);
            ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeCommandLength, command.Length);
            ShelliTelemetry.AddActive(executableName, 1);

            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = filename;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.CreateNoWindow = false;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(65001);
                    p.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(65001);

                    bool propagated = ApplyTraceContext(p.StartInfo, activity);
                    ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeContextPropagated, propagated);

                    countLines = activity != null || ShelliTelemetry.OutputLines.Enabled;
                    if (countLines)
                    {
                        p.OutputDataReceived += (a, b) => { if (b.Data != null) Interlocked.Increment(ref stdoutLines); };
                        p.ErrorDataReceived += (a, b) => { if (b.Data != null) Interlocked.Increment(ref stderrLines); };
                    }

                    if (OutputDataReceived != null) p.OutputDataReceived += (a, b) => OutputDataReceived(b.Data);
                    if (ErrorDataReceived != null) p.ErrorDataReceived += (a, b) => ErrorDataReceived(b.Data);

                    RunStage(executableName, ShelliTelemetryNames.StageStartSpan, ShelliTelemetryNames.StageStart, () =>
                    {
                        p.Start();
                    });

                    ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributePid, GetProcessId(p));

                    RunStage(executableName, ShelliTelemetryNames.StageWaitSpan, ShelliTelemetryNames.StageWait, () =>
                    {
                        p.BeginErrorReadLine();
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                    });

                    int exitCode = p.ExitCode;
                    ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeExitCode, exitCode);

                    if (exitCode == 0)
                    {
                        outcome = ShelliTelemetryNames.OutcomeSuccess;
                        ShelliTelemetry.SetOk(activity);
                    }
                    else
                    {
                        outcome = ShelliTelemetryNames.OutcomeNonZeroExit;
                        ShelliTelemetry.SetError(activity, null, "Process exited with code " + exitCode);
                    }

                    return exitCode;
                }
            }
            catch (Exception e)
            {
                outcome = ShelliTelemetryNames.OutcomeError;
                errorType = e.GetType().FullName;
                ShelliTelemetry.SetError(activity, e, e.Message);
                throw;
            }
            finally
            {
                long stdout = Interlocked.Read(ref stdoutLines);
                long stderr = Interlocked.Read(ref stderrLines);

                if (countLines)
                {
                    ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeStdoutLines, stdout);
                    ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeStderrLines, stderr);
                    ShelliTelemetry.RecordOutputLines(executableName, ShelliTelemetryNames.StreamStdout, stdout);
                    ShelliTelemetry.RecordOutputLines(executableName, ShelliTelemetryNames.StreamStderr, stderr);
                }

                ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeOutcome, outcome);
                ShelliTelemetry.RecordCommand(executableName, outcome, errorType, ShelliTelemetry.ElapsedSeconds(startTimestamp));
                ShelliTelemetry.AddActive(executableName, -1);
                ShelliTelemetry.StopActivity(activity);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    OutputDataReceived = null;
                    ErrorDataReceived = null;
                }

                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        private static void RunStage(string executableName, string spanName, string stage, Action action)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            Activity activity = ShelliTelemetry.StartActivity(spanName, ActivityKind.Internal);
            ShelliTelemetry.SetTag(activity, ShelliTelemetryNames.AttributeStage, stage);
            bool success = false;

            try
            {
                action();
                success = true;
                ShelliTelemetry.SetOk(activity);
            }
            catch (Exception e)
            {
                ShelliTelemetry.SetError(activity, e, e.Message);
                throw;
            }
            finally
            {
                ShelliTelemetry.RecordStage(executableName, stage, success, ShelliTelemetry.ElapsedSeconds(startTimestamp));
                ShelliTelemetry.StopActivity(activity);
            }
        }

        private bool ApplyTraceContext(ProcessStartInfo startInfo, Activity activity)
        {
            if (!PropagateTraceContext) return false;

            try
            {
                Activity context = activity ?? Activity.Current;
                if (context == null || context.IdFormat != ActivityIdFormat.W3C) return false;

                startInfo.Environment[ShelliTelemetryNames.TraceParentEnvironmentVariable] = context.Id;

                if (!String.IsNullOrEmpty(context.TraceStateString))
                    startInfo.Environment[ShelliTelemetryNames.TraceStateEnvironmentVariable] = context.TraceStateString;
                else
                    startInfo.Environment.Remove(ShelliTelemetryNames.TraceStateEnvironmentVariable);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetExecutableName(string filename)
        {
            try
            {
                string name = Path.GetFileName(filename);
                return String.IsNullOrEmpty(name) ? filename : name;
            }
            catch (ArgumentException)
            {
                return "unknown";
            }
        }

        private static int GetProcessId(Process p)
        {
            try
            {
                return p.Id;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        #endregion
    }
}
