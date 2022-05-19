using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HeyShelli
{
    /// <summary>
    /// Shell runner class.
    /// </summary>
    public static class Shelli
    {
        /// <summary>
        /// Action to invoke when data is received.
        /// </summary>
        public static Action<string> OutputDataReceived = null;

        /// <summary>
        /// Action to invoke when error data is received.
        /// </summary>
        public static Action<string> ErrorDataReceived = null;

        /// <summary>
        /// Execute a command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>Integer.</returns>
        public static int Go(string command)
        {
            if (String.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));

            string filename = null;
            string args = null;

            // filename  i.e. "cmd.exe"
            // args      i.e. "/c dir /w"

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filename = "cmd.exe";
                args = "/c \"" + command + "\"";
            }
            else
            {
                filename = "sh";
                args = "-c \"" + command + "\"";
            }

            Process p = new Process();
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            if (OutputDataReceived != null) p.OutputDataReceived += (a, b) => OutputDataReceived(b.Data);
            if (ErrorDataReceived != null) p.ErrorDataReceived += (a, b) => ErrorDataReceived(b.Data);

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            p.WaitForExit();
            return p.ExitCode;
        }
    }
}
