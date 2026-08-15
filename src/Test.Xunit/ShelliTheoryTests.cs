namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Touchstone.Core;

    using Test.Shared;

    using global::Xunit;
    using global::Xunit.Abstractions;

    /// <summary>
    /// Theory-driven xUnit host. Each non-skipped Shelli descriptor becomes a separate theory row.
    /// </summary>
    public sealed class ShelliTheoryTests
    {
        private readonly ITestOutputHelper _Output;

        /// <summary>
        /// Initialize with the xUnit output helper.
        /// </summary>
        /// <param name="output">Test output helper.</param>
        public ShelliTheoryTests(ITestOutputHelper output)
        {
            _Output = output;
        }

        /// <summary>
        /// Provides non-skipped test cases as theory data.
        /// </summary>
        /// <returns>Theory data rows.</returns>
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in ShelliSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip)
                        data.Add(testCase);
                }
            }

            return data;
        }

        /// <summary>
        /// Provides skipped test cases as theory data.
        /// </summary>
        /// <returns>Theory data rows.</returns>
        public static TheoryData<TestCaseDescriptor> SkippedCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in ShelliSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (testCase.Skip)
                        data.Add(testCase);
                }
            }

            return data;
        }

        /// <summary>
        /// Execute a single test case descriptor.
        /// </summary>
        /// <param name="testCase">Test case to run.</param>
        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Skipped test cases reported through xUnit's skip mechanism.
        /// </summary>
        /// <param name="testCase">Skipped test case.</param>
        [Theory(Skip = "Dynamically skipped test cases")]
        [MemberData(nameof(SkippedCases))]
        public Task Skipped(TestCaseDescriptor testCase)
        {
            _Output.WriteLine("Skipped: " + testCase.SkipReason);
            return Task.CompletedTask;
        }
    }
}
