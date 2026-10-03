namespace Test.Shared
{
    using System.Collections.Generic;

    /// <summary>
    /// One metric measurement captured by <see cref="TelemetryCapture"/>.
    /// </summary>
    public sealed class CapturedMeasurement
    {
        #region Public-Members

        /// <summary>
        /// Instrument name.
        /// </summary>
        public string Instrument { get; }

        /// <summary>
        /// Measured value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Measurement tags.
        /// </summary>
        public Dictionary<string, object> Tags { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="instrument">Instrument name.</param>
        /// <param name="value">Measured value.</param>
        /// <param name="tags">Measurement tags.</param>
        public CapturedMeasurement(string instrument, double value, Dictionary<string, object> tags)
        {
            Instrument = instrument;
            Value = value;
            Tags = tags ?? new Dictionary<string, object>();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve a tag value as a string, or null when absent.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <returns>Tag value or null.</returns>
        public string Tag(string key)
        {
            object value;
            return Tags.TryGetValue(key, out value) && value != null ? value.ToString() : null;
        }

        #endregion
    }
}
