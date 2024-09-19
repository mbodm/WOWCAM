using System.Diagnostics;

namespace WOWCAM.Helper
{
    public static class ProcessHelper
    {
        public static bool IsRunningProcess(string exeFilePath)
        {
            return GetRunningProcess(exeFilePath) != null;
        }

        public static async Task<bool> KillProcessAsync(string exeFilePath)
        {
            var process = GetRunningProcess(exeFilePath);
            if (process == null) return false;

            process.Kill();

            await Task.Delay(1000);
            return !IsRunningProcess(exeFilePath);
        }

        public static async Task<bool> StartIndependentProcessAsync(string exeFilePath)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{exeFilePath}\"",
                CreateNoWindow = true,
            };

            var process = Process.Start(processStartInfo);
            if (process == null) return false;

            await Task.Delay(3000);
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
