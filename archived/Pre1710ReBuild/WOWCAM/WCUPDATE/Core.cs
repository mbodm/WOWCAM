using System;
using System.Diagnostics;
using System.IO;

namespace WCUPDATE
{
    internal static class Core
    {
        public static string TargetFolder => Helper.GetApplicationFolder();
        public static string TargetFileName => "WOWCAM.exe";
        public static string TargetFilePath => Path.Combine(TargetFolder, TargetFileName);

        public static void ShowError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Link);
        }

        public static string EvalUpdateFolderArg(string updateFolder)
        {
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(updateFolder);

                return Path.GetFullPath(expanded);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int EvalProcessIdArg(string processId)
        {
            return int.TryParse(processId, out int result) ? result : 0;
        }

        public static bool TargetApplicationIsRunning(int processId)
        {
            try
            {
                Process.GetProcessById(processId);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CloseTargetApplication(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                return process.CloseMainWindow();
            }
            catch
            {
                return false;
            }
        }

        public static bool ReplaceTargetFile(string updateFilePath)
        {
            try
            {
                File.Copy(updateFilePath, TargetFilePath, true);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartTargetApplication()
        {
            try
            {
                return Process.Start(TargetFilePath) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
