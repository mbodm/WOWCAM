using System.Diagnostics;

namespace WOWCAM.Helper
{
    public sealed class ProcessHelper
    {
        public static bool IsRunningProcess(string exeFilePath)
        {
            if (string.IsNullOrWhiteSpace(exeFilePath))
            {
                throw new ArgumentException($"'{nameof(exeFilePath)}' cannot be null or whitespace.", nameof(exeFilePath));
            }

            return GetRunningProcess(exeFilePath) != null;
        }

        public static async Task<bool> KillProcessAsync(string exeFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(exeFilePath))
            {
                throw new ArgumentException($"'{nameof(exeFilePath)}' cannot be null or whitespace.", nameof(exeFilePath));
            }

            var process = GetRunningProcess(exeFilePath);
            if (process == null) return false;

            process.Kill();

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            return !IsRunningProcess(exeFilePath);
        }

        public static async Task<bool> StartIndependentProcessAsync(string exeFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(exeFilePath))
            {
                throw new ArgumentException($"'{nameof(exeFilePath)}' cannot be null or whitespace.", nameof(exeFilePath));
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{exeFilePath}\"",
                CreateNoWindow = true,
            };

            var process = Process.Start(processStartInfo);
            if (process == null) return false;

            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            return IsRunningProcess(exeFilePath);
        }

        private static Process? GetRunningProcess(string exeFilePath)
        {
            var name = Path.GetFileNameWithoutExtension(exeFilePath);
            var processes = Process.GetProcessesByName(name);

            return processes?.FirstOrDefault();
        }
    }
}
