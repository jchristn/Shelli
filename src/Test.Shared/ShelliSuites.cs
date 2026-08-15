namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using HeyShelli;

    using Touchstone.Core;

    /// <summary>
    /// Shared Touchstone test suite descriptors for the Shelli library.
    /// This class is the single source of truth for all Shelli test cases and is
    /// consumed by the automated CLI runner, the xUnit adapter, and the NUnit adapter.
    /// </summary>
    public static class ShelliSuites
    {
        #region Public-Members

        /// <summary>
        /// All test suites covering the Shelli library.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    DefaultsSuite(),
                    PropertiesSuite(),
                    ArgumentValidationSuite(),
                    ExecutionSuite(),
                    DisposeSuite()
                };
            }
        }

        #endregion

        #region Private-Members

        private static readonly bool _IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        #endregion

        #region Suites

        /// <summary>
        /// Verifies the documented default values of a freshly constructed Shelli instance.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor DefaultsSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Defaults",
                displayName: "Constructor Defaults",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Defaults",
                        caseId: "WindowsShellDefault",
                        displayName: "WindowsShell defaults to 'cmd.exe'",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertEqual("cmd.exe", shell.WindowsShell);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Defaults",
                        caseId: "LinuxShellDefault",
                        displayName: "LinuxShell defaults to 'sh'",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertEqual("sh", shell.LinuxShell);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Defaults",
                        caseId: "OutputHandlerDefaultNull",
                        displayName: "OutputDataReceived defaults to null",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertNull(shell.OutputDataReceived, nameof(shell.OutputDataReceived));
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Defaults",
                        caseId: "ErrorHandlerDefaultNull",
                        displayName: "ErrorDataReceived defaults to null",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertNull(shell.ErrorDataReceived, nameof(shell.ErrorDataReceived));
                            }
                            return Task.CompletedTask;
                        }),
                });
        }

        /// <summary>
        /// Exercises the property getters and setters, including validation.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor PropertiesSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Properties",
                displayName: "Property Getters, Setters, and Validation",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "WindowsShellSetPersists",
                        displayName: "WindowsShell setter stores a custom value",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                shell.WindowsShell = "powershell.exe";
                                AssertEqual("powershell.exe", shell.WindowsShell);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "LinuxShellSetPersists",
                        displayName: "LinuxShell setter stores a custom value",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                shell.LinuxShell = "bash";
                                AssertEqual("bash", shell.LinuxShell);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "WindowsShellSetNullThrows",
                        displayName: "WindowsShell setter rejects null",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.WindowsShell = null);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "WindowsShellSetEmptyThrows",
                        displayName: "WindowsShell setter rejects empty string",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.WindowsShell = "");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "LinuxShellSetNullThrows",
                        displayName: "LinuxShell setter rejects null",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.LinuxShell = null);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "LinuxShellSetEmptyThrows",
                        displayName: "LinuxShell setter rejects empty string",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.LinuxShell = "");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "OutputHandlerAssignable",
                        displayName: "OutputDataReceived can be assigned and read back",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                Action<string> handler = s => { };
                                shell.OutputDataReceived = handler;
                                AssertTrue(ReferenceEquals(handler, shell.OutputDataReceived),
                                    "OutputDataReceived did not return the assigned delegate");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Properties",
                        caseId: "ErrorHandlerAssignable",
                        displayName: "ErrorDataReceived can be assigned and read back",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                Action<string> handler = s => { };
                                shell.ErrorDataReceived = handler;
                                AssertTrue(ReferenceEquals(handler, shell.ErrorDataReceived),
                                    "ErrorDataReceived did not return the assigned delegate");
                            }
                            return Task.CompletedTask;
                        }),
                });
        }

        /// <summary>
        /// Validates argument checking on the Go method.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor ArgumentValidationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "GoValidation",
                displayName: "Go Argument Validation",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "GoValidation",
                        caseId: "GoNullThrows",
                        displayName: "Go(null) throws ArgumentNullException",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.Go(null));
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "GoValidation",
                        caseId: "GoEmptyThrows",
                        displayName: "Go(\"\") throws ArgumentNullException",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                AssertThrows<ArgumentNullException>(() => shell.Go(""));
                            }
                            return Task.CompletedTask;
                        }),
                });
        }

        /// <summary>
        /// Exercises real command execution, exit codes, and output/error stream capture.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor ExecutionSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Execution",
                displayName: "Command Execution",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "SuccessfulCommandReturnsZero",
                        displayName: "A successful command returns exit code 0",
                        executeAsync: ct =>
                        {
                            RunResult result = Run(EchoCommand("shelli_ok"));
                            AssertEqual(0, result.ExitCode);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "StdOutIsCaptured",
                        displayName: "Standard output is delivered to OutputDataReceived",
                        executeAsync: ct =>
                        {
                            RunResult result = Run(EchoCommand("shelli_token_out"));
                            AssertEqual(0, result.ExitCode);
                            AssertContains(result.StdOut, "shelli_token_out");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "StdErrIsCaptured",
                        displayName: "Standard error is delivered to ErrorDataReceived",
                        executeAsync: ct =>
                        {
                            RunResult result = Run(EchoToStdErrCommand("shelli_token_err"));
                            AssertContains(result.StdErr, "shelli_token_err");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "ExplicitZeroExit",
                        displayName: "'exit 0' yields exit code 0",
                        executeAsync: ct =>
                        {
                            RunResult result = Run("exit 0");
                            AssertEqual(0, result.ExitCode);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "NonZeroExitCodeIsPropagated",
                        displayName: "'exit 5' propagates exit code 5",
                        executeAsync: ct =>
                        {
                            RunResult result = Run("exit 5");
                            AssertEqual(5, result.ExitCode);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "UnknownCommandReturnsNonZero",
                        displayName: "An unknown command returns a non-zero exit code",
                        executeAsync: ct =>
                        {
                            RunResult result = Run("shelli_no_such_command_9f3a1");
                            AssertNotEqual(0, result.ExitCode);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "RunsWithoutHandlers",
                        displayName: "Go executes with no output/error handlers attached",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                int rc = shell.Go(EchoCommand("no_handlers"));
                                AssertEqual(0, rc);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "InstanceIsReusable",
                        displayName: "A single Shelli instance can run multiple commands",
                        executeAsync: ct =>
                        {
                            using (Shelli shell = new Shelli())
                            {
                                int first = shell.Go(EchoCommand("first"));
                                int second = shell.Go(EchoCommand("second"));
                                AssertEqual(0, first);
                                AssertEqual(0, second);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "CustomShellExecutes",
                        displayName: "An explicitly configured shell still executes commands",
                        executeAsync: ct =>
                        {
                            StringBuilder outBuilder = new StringBuilder();
                            object sync = new object();

                            using (Shelli shell = new Shelli())
                            {
                                if (_IsWindows) shell.WindowsShell = "cmd.exe";
                                else shell.LinuxShell = "sh";

                                shell.OutputDataReceived = s =>
                                {
                                    if (s == null) return;
                                    lock (sync) outBuilder.AppendLine(s);
                                };

                                int rc = shell.Go(EchoCommand("custom_shell_token"));
                                AssertEqual(0, rc);
                            }

                            AssertContains(outBuilder.ToString(), "custom_shell_token");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "MultipleOutputLinesCaptured",
                        displayName: "Multiple output lines are all captured",
                        executeAsync: ct =>
                        {
                            string command = _IsWindows
                                ? "echo line_one && echo line_two"
                                : "echo line_one && echo line_two";

                            RunResult result = Run(command);
                            AssertEqual(0, result.ExitCode);
                            AssertContains(result.StdOut, "line_one");
                            AssertContains(result.StdOut, "line_two");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Execution",
                        caseId: "PaginationPlaceholder",
                        displayName: "Placeholder for future streaming API",
                        skip: true,
                        skipReason: "Streaming output API not yet implemented",
                        executeAsync: _ => Task.CompletedTask),
                });
        }

        /// <summary>
        /// Validates IDisposable behavior.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor DisposeSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Dispose",
                displayName: "Disposal Behavior",
                afterSuiteAsync: _ => new ValueTask(),
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Dispose",
                        caseId: "DisposeDoesNotThrow",
                        displayName: "Dispose completes without throwing",
                        executeAsync: ct =>
                        {
                            Shelli shell = new Shelli();
                            shell.Dispose();
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Dispose",
                        caseId: "DisposeIsIdempotent",
                        displayName: "Dispose can be called multiple times safely",
                        executeAsync: ct =>
                        {
                            Shelli shell = new Shelli();
                            shell.Dispose();
                            shell.Dispose();
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Dispose",
                        caseId: "DisposeClearsHandlers",
                        displayName: "Dispose clears output and error handlers",
                        executeAsync: ct =>
                        {
                            Shelli shell = new Shelli();
                            shell.OutputDataReceived = s => { };
                            shell.ErrorDataReceived = s => { };
                            shell.Dispose();
                            AssertNull(shell.OutputDataReceived, nameof(shell.OutputDataReceived));
                            AssertNull(shell.ErrorDataReceived, nameof(shell.ErrorDataReceived));
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor(
                        suiteId: "Dispose",
                        caseId: "UsingBlockPattern",
                        displayName: "Shelli works correctly inside a using block",
                        executeAsync: ct =>
                        {
                            int rc;
                            using (Shelli shell = new Shelli())
                            {
                                rc = shell.Go(EchoCommand("using_block"));
                            }
                            AssertEqual(0, rc);
                            return Task.CompletedTask;
                        }),
                });
        }

        #endregion

        #region Command-Builders

        private static string EchoCommand(string token)
        {
            return "echo " + token;
        }

        private static string EchoToStdErrCommand(string token)
        {
            // '1>&2' redirects stdout to stderr on both cmd.exe and POSIX shells.
            return "echo " + token + " 1>&2";
        }

        #endregion

        #region Run-Helpers

        /// <summary>
        /// Runs a command through a fresh Shelli instance, capturing stdout, stderr, and exit code.
        /// </summary>
        private static RunResult Run(string command)
        {
            StringBuilder outBuilder = new StringBuilder();
            StringBuilder errBuilder = new StringBuilder();
            object sync = new object();

            int exitCode;
            using (Shelli shell = new Shelli())
            {
                shell.OutputDataReceived = s =>
                {
                    if (s == null) return;
                    lock (sync) outBuilder.AppendLine(s);
                };

                shell.ErrorDataReceived = s =>
                {
                    if (s == null) return;
                    lock (sync) errBuilder.AppendLine(s);
                };

                exitCode = shell.Go(command);
            }

            lock (sync)
            {
                return new RunResult(exitCode, outBuilder.ToString(), errBuilder.ToString());
            }
        }

        private sealed class RunResult
        {
            public int ExitCode { get; }
            public string StdOut { get; }
            public string StdErr { get; }

            public RunResult(int exitCode, string stdOut, string stdErr)
            {
                ExitCode = exitCode;
                StdOut = stdOut;
                StdErr = stdErr;
            }
        }

        #endregion

        #region Assertions

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'");
        }

        private static void AssertNotEqual<T>(T notExpected, T actual)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new InvalidOperationException("Value was not expected to equal '" + notExpected + "'");
        }

        private static void AssertNull(object value, string name)
        {
            if (value != null)
                throw new InvalidOperationException("Expected '" + name + "' to be null but it was not");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertContains(string haystack, string needle)
        {
            if (haystack == null || haystack.IndexOf(needle, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Expected output to contain '" + needle + "' but it was: '" + (haystack ?? "<null>").Trim() + "'");
        }

        private static void AssertThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Expected " + typeof(TException).Name + " but got " + ex.GetType().Name);
            }

            throw new InvalidOperationException(
                "Expected " + typeof(TException).Name + " but no exception was thrown");
        }

        #endregion
    }
}
