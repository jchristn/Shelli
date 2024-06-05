using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HeyShelli
{
    /// <summary>
    /// Shell runner class.
    /// </summary>
    public static class Shelli
    {
        #region Public-Members

        /// <summary>
        /// Action to invoke when data is received.
        /// </summary>
        public static Action<string> OutputDataReceived = null;

        /// <summary>
        /// Action to invoke when error data is received.
        /// </summary>
        public static Action<string> ErrorDataReceived = null;

        /// <summary>
        /// Windows shell command.  Defaults to 'cmd.exe'.  For certain commands and environments, it may be necessary to change this value.
        /// </summary>
        public static string WindowsShell
        {
            get
            {
                return _WindowsShell;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(WindowsShell));
                _WindowsShell = value;
            }
        }

        /// <summary>
        /// Linux shell command.  Defaults to 'sh'.  For certain commands and environments, you may need to change this.  'bash' is a common alternative.
        /// </summary>
        public static string LinuxShell
        {
            get
            {
                return _LinuxShell;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(LinuxShell));
                _LinuxShell = value;
            }
        }

        #endregion

        #region Private-Members

        private static string _WindowsShell = "cmd.exe";
        private static string _LinuxShell = "sh";

        #endregion

        #region Public-Methods

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
                filename = WindowsShell;
                args = "/c \"" + command + "\"";
            }
            else
            {
                filename = LinuxShell;
                args = "-c \"" + command + "\"";
            }

            Process p = new Process();
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(65001);
            p.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(65001);

            if (OutputDataReceived != null) p.OutputDataReceived += (a, b) => OutputDataReceived(b.Data);
            if (ErrorDataReceived != null) p.ErrorDataReceived += (a, b) => ErrorDataReceived(b.Data);

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            p.WaitForExit();
            return p.ExitCode;
        }

        #endregion
    }
}
