using System.Threading.Tasks;

using Touchstone.Cli;

using Test.Shared;

namespace Test.Automated
{
    /// <summary>
    /// Touchstone CLI runner for the Shelli test suites.
    /// Runs every descriptor defined in <see cref="ShelliSuites"/> and returns a
    /// non-zero exit code if any test fails.
    /// Usage: dotnet run [--results &lt;path&gt;]
    /// </summary>
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            string resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--results" && i + 1 < args.Length)
                {
                    resultsPath = args[i + 1];
                    break;
                }
            }

            return await ConsoleRunner.RunAsync(ShelliSuites.All, resultsPath: resultsPath);
        }
    }
}
