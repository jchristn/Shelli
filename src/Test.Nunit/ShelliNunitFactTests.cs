namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    using Test.Shared;

    /// <summary>
    /// NUnit fact-style host. All Shelli descriptors run in a single [Test].
    /// </summary>
    [TestFixture]
    public sealed class ShelliNunitFactTests : TouchstoneNunitBase
    {
        /// <inheritdoc />
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return ShelliSuites.All; }
        }

        [Test]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
