namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;

    using HeyShelli;

    /// <summary>
    /// In-memory collector for Shelli spans and metrics.
    /// Starts a test-owned root activity on construction and keeps only telemetry that belongs to that trace,
    /// so captures stay isolated when other tests run Shelli concurrently.
    /// </summary>
    public sealed class TelemetryCapture : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Name of the activity source used for the test-owned root activity.
        /// </summary>
        public const string TestSourceName = "Shelli.Tests";

        /// <summary>
        /// Test-owned root activity. Shelli spans created while it is current become its descendants.
        /// </summary>
        public Activity Root
        {
            get { return _Root; }
        }

        /// <summary>
        /// When true, the meter and activity listeners throw from their callbacks for this trace, to prove instrumentation is best-effort.
        /// </summary>
        public bool ThrowFromListeners { get; set; } = false;

        #endregion

        #region Private-Members

        private static readonly ActivitySource _TestSource = new ActivitySource(TestSourceName);

        private readonly object _Lock = new object();
        private readonly List<Activity> _Activities = new List<Activity>();
        private readonly List<CapturedMeasurement> _Measurements = new List<CapturedMeasurement>();
        private readonly ActivityListener _ActivityListener;
        private readonly MeterListener _MeterListener;
        private readonly Activity _Root;
        private readonly ActivityTraceId _TraceId;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate and start listening.
        /// </summary>
        public TelemetryCapture()
        {
            _ActivityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ShelliTelemetryNames.ActivitySourceName || source.Name == TestSourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = OnActivityStopped
            };
            ActivitySource.AddActivityListener(_ActivityListener);

            _MeterListener = new MeterListener();
            _MeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ShelliTelemetryNames.MeterName) listener.EnableMeasurementEvents(instrument);
            };
            _MeterListener.SetMeasurementEventCallback<double>((i, v, t, s) => OnMeasurement(i, v, t));
            _MeterListener.SetMeasurementEventCallback<long>((i, v, t, s) => OnMeasurement(i, v, t));
            _MeterListener.SetMeasurementEventCallback<int>((i, v, t, s) => OnObservable(i, v, t));
            _MeterListener.Start();

            _Root = _TestSource.StartActivity("test root", ActivityKind.Internal);
            if (_Root == null) throw new InvalidOperationException("Test root activity was not created");
            _TraceId = _Root.TraceId;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Stop the root activity so later work is no longer attributed to this capture.
        /// </summary>
        public void EndRoot()
        {
            if (_Root != null && _Root.Duration == TimeSpan.Zero) _Root.Stop();
        }

        /// <summary>
        /// Shelli spans in this trace with the given name.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <returns>Matching spans.</returns>
        public List<Activity> Spans(string name)
        {
            lock (_Lock)
            {
                return _Activities.Where(a => a.Source.Name == ShelliTelemetryNames.ActivitySourceName && a.DisplayName == name).ToList();
            }
        }

        /// <summary>
        /// Measurements in this trace for the given instrument.
        /// </summary>
        /// <param name="instrument">Instrument name.</param>
        /// <returns>Matching measurements.</returns>
        public List<CapturedMeasurement> Measurements(string instrument)
        {
            lock (_Lock)
            {
                return _Measurements.Where(m => m.Instrument == instrument).ToList();
            }
        }

        /// <summary>
        /// Collect observable instruments (for example the library info gauge) into this capture.
        /// </summary>
        public void CollectObservables()
        {
            _MeterListener.RecordObservableInstruments();
        }

        /// <summary>
        /// Dispose listeners and the root activity.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            EndRoot();
            _MeterListener.Dispose();
            _ActivityListener.Dispose();
        }

        #endregion

        #region Private-Methods

        private bool InTrace()
        {
            Activity current = Activity.Current;
            return current != null && current.TraceId == _TraceId;
        }

        private void OnActivityStopped(Activity activity)
        {
            if (activity.TraceId != _TraceId) return;
            lock (_Lock) _Activities.Add(activity);
            if (ThrowFromListeners) throw new InvalidOperationException("Intentional activity listener failure");
        }

        private void OnMeasurement(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            if (!InTrace()) return;
            CapturedMeasurement m = new CapturedMeasurement(instrument.Name, value, ToDictionary(tags));
            lock (_Lock) _Measurements.Add(m);
            if (ThrowFromListeners) throw new InvalidOperationException("Intentional meter listener failure");
        }

        private void OnObservable(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            CapturedMeasurement m = new CapturedMeasurement(instrument.Name, value, ToDictionary(tags));
            lock (_Lock) _Measurements.Add(m);
        }

        private static Dictionary<string, object> ToDictionary(ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> tag in tags) dict[tag.Key] = tag.Value;
            return dict;
        }

        #endregion
    }
}
