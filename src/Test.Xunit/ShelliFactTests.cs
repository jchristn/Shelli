namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    using Test.Shared;

    using global::Xunit;

    /// <summary>
    /// Fact-style xUnit host. All Shelli descriptors run in a single [Fact].
    /// </summary>
    public sealed class ShelliFactTests : TouchstoneFactBase
    {
        /// <inheritdoc />
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return ShelliSuites.All; }
        }

        /// <summary>
        /// Run all shared descriptors as a single fact.
        /// </summary>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
