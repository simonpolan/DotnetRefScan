using System.Diagnostics;
using System.Threading.Tasks;

namespace DotnetRefScan
{
    /// <summary>
    /// Process executor.
    /// </summary>
    public static class ProcessExecutor
    {
        /// <summary>
        /// Runs a specified process and waits for it to exit.
        /// </summary>
        /// <param name="fileName">Process file name.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="workingDirectory">Working directory.</param>
        public static async Task RunProcess(string fileName, string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = new Process { StartInfo = psi };

            process.Start();

            process.WaitForExit();
        }


        /// <summary>
        /// Runs a specified process, waits for it to exit and reads the output and error streams.
        /// </summary>
        /// <param name="fileName">Process file name.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="workingDirectory">Working directory.</param>
        public static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessWithOutput(string fileName, string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = new Process { StartInfo = psi };

            process.Start();

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            return (
                process.ExitCode,
                await stdoutTask.ConfigureAwait(false),
                await stderrTask.ConfigureAwait(false)
            );
        }
    }
}
