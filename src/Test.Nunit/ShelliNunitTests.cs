namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    using Test.Shared;

    /// <summary>
    /// NUnit host using TestCaseSource for data-driven execution. Each non-skipped
    /// Shelli descriptor becomes a separate NUnit test case.
    /// </summary>
    [TestFixture]
    public sealed class ShelliNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(ShelliSuites.All);
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
