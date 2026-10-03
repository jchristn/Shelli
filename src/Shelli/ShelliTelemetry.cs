namespace HeyShelli
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Reflection;

    /// <summary>
    /// Process-wide Shelli meter, activity source, and instruments.
    /// Every method is best-effort: a failure inside a listener never propagates to the caller.
    /// </summary>
    internal static class ShelliTelemetry
    {
        #region Internal-Members

        internal static readonly string Version = GetVersion();

        internal static readonly ActivitySource Source = new ActivitySource(ShelliTelemetryNames.ActivitySourceName, Version);

        internal static readonly Meter Meter = new Meter(ShelliTelemetryNames.MeterName, Version);

        internal static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
            ShelliTelemetryNames.CommandDuration, "s", "End-to-end duration of one shell command execution.");

        internal static readonly Counter<long> CommandExecutions = Meter.CreateCounter<long>(
            ShelliTelemetryNames.CommandExecutions, "{command}", "Completed shell command executions by outcome.");

        internal static readonly UpDownCounter<long> CommandActive = Meter.CreateUpDownCounter<long>(
            ShelliTelemetryNames.CommandActive, "{command}", "Shell commands currently executing.");

        internal static readonly Histogram<double> StageDuration = Meter.CreateHistogram<double>(
            ShelliTelemetryNames.StageDuration, "s", "Duration of each shell command execution stage.");

        internal static readonly Counter<long> OutputLines = Meter.CreateCounter<long>(
            ShelliTelemetryNames.OutputLines, "{line}", "Lines read from child process output streams.");

        internal static readonly ObservableGauge<int> LibraryInfo = Meter.CreateObservableGauge<int>(
            ShelliTelemetryNames.LibraryInfo,
            () => new Measurement<int>(1, new KeyValuePair<string, object>(ShelliTelemetryNames.AttributeVersion, Version)),
            "{info}",
            "Shelli library build information.");

        #endregion

        #region Internal-Methods

        internal static Activity StartActivity(string name, ActivityKind kind)
        {
            try
            {
                if (!Source.HasListeners()) return null;
                return Source.StartActivity(name, kind);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void StopActivity(Activity activity)
        {
            if (activity == null) return;

            try
            {
                activity.Dispose();
            }
            catch (Exception)
            {
                // A throwing listener can abort Stop before the ambient context is restored.
                try
                {
                    if (Activity.Current == activity) Activity.Current = activity.Parent;
                }
                catch (Exception)
                {
                }
            }
        }

        internal static void SetTag(Activity activity, string key, object value)
        {
            if (activity == null) return;

            try
            {
                activity.SetTag(key, value);
            }
            catch (Exception)
            {
            }
        }

        internal static void SetError(Activity activity, Exception exception, string description)
        {
            if (activity == null) return;

            try
            {
                activity.SetStatus(ActivityStatusCode.Error, description);

                if (exception != null)
                {
                    activity.SetTag(ShelliTelemetryNames.AttributeErrorType, exception.GetType().FullName);

                    ActivityTagsCollection tags = new ActivityTagsCollection
                    {
                        { "exception.type", exception.GetType().FullName },
                        { "exception.message", exception.Message },
                        { "exception.stacktrace", exception.ToString() }
                    };

                    activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void SetOk(Activity activity)
        {
            if (activity == null) return;

            try
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception)
            {
            }
        }

        internal static void AddActive(string executableName, long delta)
        {
            try
            {
                if (!CommandActive.Enabled) return;
                CommandActive.Add(delta, new KeyValuePair<string, object>(ShelliTelemetryNames.AttributeExecutableName, executableName));
            }
            catch (Exception)
            {
            }
        }

        internal static void RecordCommand(string executableName, string outcome, string errorType, double seconds)
        {
            try
            {
                if (!CommandDuration.Enabled && !CommandExecutions.Enabled) return;

                TagList tags = new TagList();
                tags.Add(ShelliTelemetryNames.AttributeExecutableName, executableName);
                tags.Add(ShelliTelemetryNames.AttributeOutcome, outcome);
                if (errorType != null) tags.Add(ShelliTelemetryNames.AttributeErrorType, errorType);

                CommandDuration.Record(seconds, tags);
                CommandExecutions.Add(1, tags);
            }
            catch (Exception)
            {
            }
        }

        internal static void RecordStage(string executableName, string stage, bool success, double seconds)
        {
            try
            {
                if (!StageDuration.Enabled) return;

                TagList tags = new TagList();
                tags.Add(ShelliTelemetryNames.AttributeExecutableName, executableName);
                tags.Add(ShelliTelemetryNames.AttributeStage, stage);
                tags.Add(ShelliTelemetryNames.AttributeOutcome, success ? ShelliTelemetryNames.OutcomeSuccess : ShelliTelemetryNames.OutcomeError);

                StageDuration.Record(seconds, tags);
            }
            catch (Exception)
            {
            }
        }

        internal static void RecordOutputLines(string executableName, string stream, long lines)
        {
            if (lines < 1) return;

            try
            {
                if (!OutputLines.Enabled) return;

                OutputLines.Add(
                    lines,
                    new KeyValuePair<string, object>(ShelliTelemetryNames.AttributeExecutableName, executableName),
                    new KeyValuePair<string, object>(ShelliTelemetryNames.AttributeStream, stream));
            }
            catch (Exception)
            {
            }
        }

        internal static double ElapsedSeconds(long startTimestamp)
        {
            return (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency;
        }

        #endregion

        #region Private-Methods

        private static string GetVersion()
        {
            try
            {
                Assembly assembly = typeof(ShelliTelemetry).Assembly;
                AssemblyInformationalVersionAttribute info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                string version = info != null ? info.InformationalVersion : null;

                if (String.IsNullOrEmpty(version))
                {
                    Version assemblyVersion = assembly.GetName().Version;
                    version = assemblyVersion != null ? assemblyVersion.ToString() : "unknown";
                }

                int plus = version.IndexOf('+');
                if (plus > 0) version = version.Substring(0, plus);
                return version;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        #endregion
    }
}
