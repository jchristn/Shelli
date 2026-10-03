namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    using HeyShelli;

    using Touchstone.Core;

    /// <summary>
    /// Touchstone suite proving Shelli emits its documented spans and metrics, including failure paths,
    /// and that instrumentation never breaks command execution.
    /// </summary>
    public static class ShelliTelemetrySuite
    {
        #region Private-Members

        private const string SuiteId = "Telemetry";
        private const string MissingShell = "shelli_missing_shell_7c2e";

        private static readonly bool _IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the telemetry suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor Suite()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Telemetry (Meter and ActivitySource)",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    Case("NamesAreStable", "Meter and activity source names are 'Shelli'", NamesAreStable),
                    Case("SuccessEmitsSpan", "Successful command emits an Ok 'shelli exec' span with stage children", SuccessEmitsSpan),
                    Case("SuccessEmitsMetrics", "Successful command records duration, executions, active, stage, and output-line metrics", SuccessEmitsMetrics),
                    Case("NonZeroExitIsError", "Non-zero exit marks the span Error and records outcome 'nonzero_exit'", NonZeroExitIsError),
                    Case("StartFailureIsRecorded", "A shell that cannot start records an exception, error.type, and a failed 'start' stage", StartFailureIsRecorded),
                    Case("StderrLinesCounted", "Lines written to stderr are counted under shelli.stream=stderr", StderrLinesCounted),
                    Case("TraceContextPropagated", "TRACEPARENT handed to the child carries the exec span id", TraceContextPropagated),
                    Case("TraceContextPropagationDisabled", "PropagateTraceContext=false leaves TRACEPARENT unset in the child", TraceContextPropagationDisabled),
                    Case("CommandTextNotRecorded", "The command text never appears in span tags or metric labels", CommandTextNotRecorded),
                    Case("LibraryInfoGauge", "shelli.library.info reports 1 with the library version", LibraryInfoGauge),
                    Case("NoListenerDoesNotThrow", "Go works with no telemetry listener attached", NoListenerDoesNotThrow),
                    Case("ThrowingListenerDoesNotBreakGo", "A listener that throws never breaks command execution", ThrowingListenerDoesNotBreakGo),
                });
        }

        #endregion

        #region Cases

        private static void NamesAreStable()
        {
            AssertEqual("Shelli", ShelliTelemetryNames.MeterName);
            AssertEqual("Shelli", ShelliTelemetryNames.ActivitySourceName);
        }

        private static void SuccessEmitsSpan()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                int rc = Go(capture, "echo telemetry_ok", null);
                AssertEqual(0, rc);

                Activity exec = Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span");
                AssertEqual(ActivityStatusCode.Ok, exec.Status);
                AssertEqual(capture.Root.SpanId, exec.ParentSpanId);
                AssertEqual(ShelliTelemetryNames.OutcomeSuccess, (string)exec.GetTagItem(ShelliTelemetryNames.AttributeOutcome));
                AssertEqual(0, (int)exec.GetTagItem(ShelliTelemetryNames.AttributeExitCode));
                AssertEqual(ExpectedShellName(), (string)exec.GetTagItem(ShelliTelemetryNames.AttributeExecutableName));
                AssertTrue((int)exec.GetTagItem(ShelliTelemetryNames.AttributePid) > 0, "process.pid not set");
                AssertEqual(1L, (long)exec.GetTagItem(ShelliTelemetryNames.AttributeStdoutLines));

                Activity start = Single(capture.Spans(ShelliTelemetryNames.StageStartSpan), "stage:start span");
                Activity wait = Single(capture.Spans(ShelliTelemetryNames.StageWaitSpan), "stage:wait span");
                AssertEqual(exec.SpanId, start.ParentSpanId);
                AssertEqual(exec.SpanId, wait.ParentSpanId);
                AssertEqual(ActivityStatusCode.Ok, start.Status);
                AssertEqual(ActivityStatusCode.Ok, wait.Status);
            }
        }

        private static void SuccessEmitsMetrics()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                Go(capture, "echo telemetry_metrics", null);

                CapturedMeasurement duration = Single(capture.Measurements(ShelliTelemetryNames.CommandDuration), "duration");
                AssertEqual(ShelliTelemetryNames.OutcomeSuccess, duration.Tag(ShelliTelemetryNames.AttributeOutcome));
                AssertEqual(ExpectedShellName(), duration.Tag(ShelliTelemetryNames.AttributeExecutableName));
                AssertNull(duration.Tag(ShelliTelemetryNames.AttributeErrorType), "error.type");
                AssertTrue(duration.Value > 0, "duration should be positive");

                CapturedMeasurement executions = Single(capture.Measurements(ShelliTelemetryNames.CommandExecutions), "executions");
                AssertEqual(1.0, executions.Value);

                List<CapturedMeasurement> active = capture.Measurements(ShelliTelemetryNames.CommandActive);
                AssertEqual(2, active.Count);
                AssertEqual(0.0, active.Sum(m => m.Value));

                List<CapturedMeasurement> stages = capture.Measurements(ShelliTelemetryNames.StageDuration);
                AssertEqual(2, stages.Count);
                AssertTrue(stages.Any(m => m.Tag(ShelliTelemetryNames.AttributeStage) == ShelliTelemetryNames.StageStart), "start stage missing");
                AssertTrue(stages.Any(m => m.Tag(ShelliTelemetryNames.AttributeStage) == ShelliTelemetryNames.StageWait), "wait stage missing");
                AssertTrue(stages.All(m => m.Tag(ShelliTelemetryNames.AttributeOutcome) == ShelliTelemetryNames.OutcomeSuccess), "stage outcome should be success");

                CapturedMeasurement lines = Single(capture.Measurements(ShelliTelemetryNames.OutputLines), "output lines");
                AssertEqual(ShelliTelemetryNames.StreamStdout, lines.Tag(ShelliTelemetryNames.AttributeStream));
                AssertEqual(1.0, lines.Value);
            }
        }

        private static void NonZeroExitIsError()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                int rc = Go(capture, "exit 5", null);
                AssertEqual(5, rc);

                Activity exec = Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span");
                AssertEqual(ActivityStatusCode.Error, exec.Status);
                AssertEqual(5, (int)exec.GetTagItem(ShelliTelemetryNames.AttributeExitCode));
                AssertEqual(ShelliTelemetryNames.OutcomeNonZeroExit, (string)exec.GetTagItem(ShelliTelemetryNames.AttributeOutcome));

                CapturedMeasurement executions = Single(capture.Measurements(ShelliTelemetryNames.CommandExecutions), "executions");
                AssertEqual(ShelliTelemetryNames.OutcomeNonZeroExit, executions.Tag(ShelliTelemetryNames.AttributeOutcome));
            }
        }

        private static void StartFailureIsRecorded()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                bool threw = false;
                try
                {
                    using (Shelli shell = new Shelli())
                    {
                        shell.WindowsShell = MissingShell;
                        shell.LinuxShell = MissingShell;
                        shell.Go("echo unreachable");
                    }
                }
                catch (Win32Exception)
                {
                    threw = true;
                }

                capture.EndRoot();
                AssertTrue(threw, "Expected Win32Exception for a missing shell");

                Activity exec = Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span");
                AssertEqual(ActivityStatusCode.Error, exec.Status);
                AssertEqual(typeof(Win32Exception).FullName, (string)exec.GetTagItem(ShelliTelemetryNames.AttributeErrorType));
                AssertEqual(ShelliTelemetryNames.OutcomeError, (string)exec.GetTagItem(ShelliTelemetryNames.AttributeOutcome));
                AssertTrue(exec.Events.Any(e => e.Name == "exception"), "exception event missing on exec span");

                Activity start = Single(capture.Spans(ShelliTelemetryNames.StageStartSpan), "stage:start span");
                AssertEqual(ActivityStatusCode.Error, start.Status);
                AssertEqual(0, capture.Spans(ShelliTelemetryNames.StageWaitSpan).Count);

                CapturedMeasurement executions = Single(capture.Measurements(ShelliTelemetryNames.CommandExecutions), "executions");
                AssertEqual(ShelliTelemetryNames.OutcomeError, executions.Tag(ShelliTelemetryNames.AttributeOutcome));
                AssertEqual(typeof(Win32Exception).FullName, executions.Tag(ShelliTelemetryNames.AttributeErrorType));
                AssertEqual(MissingShell, executions.Tag(ShelliTelemetryNames.AttributeExecutableName));

                CapturedMeasurement stage = Single(capture.Measurements(ShelliTelemetryNames.StageDuration), "stage");
                AssertEqual(ShelliTelemetryNames.StageStart, stage.Tag(ShelliTelemetryNames.AttributeStage));
                AssertEqual(ShelliTelemetryNames.OutcomeError, stage.Tag(ShelliTelemetryNames.AttributeOutcome));

                AssertEqual(0.0, capture.Measurements(ShelliTelemetryNames.CommandActive).Sum(m => m.Value));
            }
        }

        private static void StderrLinesCounted()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                Go(capture, "echo telemetry_err 1>&2", null);

                CapturedMeasurement lines = Single(capture.Measurements(ShelliTelemetryNames.OutputLines), "output lines");
                AssertEqual(ShelliTelemetryNames.StreamStderr, lines.Tag(ShelliTelemetryNames.AttributeStream));
                AssertEqual(1.0, lines.Value);
            }
        }

        private static void TraceContextPropagated()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                StringBuilder output = new StringBuilder();
                Go(capture, EchoTraceParent(), output);

                Activity exec = Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span");
                AssertEqual(true, (bool)exec.GetTagItem(ShelliTelemetryNames.AttributeContextPropagated));
                AssertContains(output.ToString(), exec.Id);
            }
        }

        private static void TraceContextPropagationDisabled()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                StringBuilder output = new StringBuilder();
                using (Shelli shell = new Shelli())
                {
                    shell.PropagateTraceContext = false;
                    shell.OutputDataReceived = s => { if (s != null) lock (output) output.AppendLine(s); };
                    shell.Go(EchoTraceParent());
                }

                capture.EndRoot();

                Activity exec = Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span");
                AssertEqual(false, (bool)exec.GetTagItem(ShelliTelemetryNames.AttributeContextPropagated));
                AssertTrue(output.ToString().IndexOf(exec.TraceId.ToHexString(), StringComparison.Ordinal) < 0,
                    "TRACEPARENT should not be set when PropagateTraceContext is false");
            }
        }

        private static void CommandTextNotRecorded()
        {
            const string secret = "shelli_secret_token_51ab";

            using (TelemetryCapture capture = new TelemetryCapture())
            {
                Go(capture, "echo " + secret, null);

                foreach (Activity a in capture.Spans(ShelliTelemetryNames.ExecSpan)
                    .Concat(capture.Spans(ShelliTelemetryNames.StageStartSpan))
                    .Concat(capture.Spans(ShelliTelemetryNames.StageWaitSpan)))
                {
                    foreach (KeyValuePair<string, object> tag in a.TagObjects)
                    {
                        AssertTrue(tag.Value == null || tag.Value.ToString().IndexOf(secret, StringComparison.Ordinal) < 0,
                            "Command text leaked into span tag " + tag.Key);
                    }
                }

                foreach (string instrument in new[] { ShelliTelemetryNames.CommandDuration, ShelliTelemetryNames.CommandExecutions, ShelliTelemetryNames.StageDuration, ShelliTelemetryNames.OutputLines })
                {
                    foreach (CapturedMeasurement m in capture.Measurements(instrument))
                    {
                        AssertTrue(m.Tags.Values.All(v => v == null || v.ToString().IndexOf(secret, StringComparison.Ordinal) < 0),
                            "Command text leaked into metric label on " + instrument);
                    }
                }

                AssertEqual(("echo " + secret).Length, (int)Single(capture.Spans(ShelliTelemetryNames.ExecSpan), "exec span").GetTagItem(ShelliTelemetryNames.AttributeCommandLength));
            }
        }

        private static void LibraryInfoGauge()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                capture.CollectObservables();
                CapturedMeasurement info = Single(capture.Measurements(ShelliTelemetryNames.LibraryInfo), "library info");
                AssertEqual(1.0, info.Value);
                AssertTrue(!String.IsNullOrEmpty(info.Tag(ShelliTelemetryNames.AttributeVersion)), "shelli.version label missing");
            }
        }

        private static void NoListenerDoesNotThrow()
        {
            using (Shelli shell = new Shelli())
            {
                AssertEqual(0, shell.Go("echo no_listener"));
                AssertEqual(3, shell.Go("exit 3"));
            }
        }

        private static void ThrowingListenerDoesNotBreakGo()
        {
            using (TelemetryCapture capture = new TelemetryCapture())
            {
                capture.ThrowFromListeners = true;
                StringBuilder output = new StringBuilder();
                int rc;

                using (Shelli shell = new Shelli())
                {
                    shell.OutputDataReceived = s => { if (s != null) lock (output) output.AppendLine(s); };
                    rc = shell.Go("echo still_works");
                }

                capture.ThrowFromListeners = false;
                AssertEqual(0, rc);
                AssertContains(output.ToString(), "still_works");
                AssertTrue(Activity.Current == capture.Root, "Ambient activity was not restored after a listener failure");
            }
        }

        #endregion

        #region Helpers

        private static TestCaseDescriptor Case(string caseId, string displayName, Action body)
        {
            return new TestCaseDescriptor(
                suiteId: SuiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: ct =>
                {
                    body();
                    return Task.CompletedTask;
                });
        }

        private static int Go(TelemetryCapture capture, string command, StringBuilder output)
        {
            int rc;
            using (Shelli shell = new Shelli())
            {
                if (output != null) shell.OutputDataReceived = s => { if (s != null) lock (output) output.AppendLine(s); };
                rc = shell.Go(command);
            }

            capture.EndRoot();
            return rc;
        }

        private static string EchoTraceParent()
        {
            return _IsWindows ? "echo %TRACEPARENT%" : "echo $TRACEPARENT";
        }

        private static string ExpectedShellName()
        {
            return _IsWindows ? "cmd.exe" : "sh";
        }

        private static T Single<T>(List<T> items, string what)
        {
            if (items.Count != 1)
                throw new InvalidOperationException("Expected exactly one " + what + " but found " + items.Count);
            return items[0];
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'");
        }

        private static void AssertNull(object value, string name)
        {
            if (value != null)
                throw new InvalidOperationException("Expected '" + name + "' to be null but it was '" + value + "'");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertContains(string haystack, string needle)
        {
            if (haystack == null || haystack.IndexOf(needle, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Expected output to contain '" + needle + "' but it was: '" + (haystack ?? "<null>").Trim() + "'");
        }

        #endregion
    }
}
