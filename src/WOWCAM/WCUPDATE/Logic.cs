using System;
using System.Diagnostics;
using System.IO;

namespace WCUPDATE
{
    internal static class Logic
    {
        public static string AppFileName => $"{Helper.GetAssemblyName()}.exe";
        public static string AppVersion => Helper.GetApplicationVersion();
        public static string AppTitle => $"{AppFileName} {AppVersion} (by MBODM 2024)";
        public static string AppDescription => $"A tiny Windows command-line tool, used by {TargetFileName} to replace it's own executable.";
        public static string AppUsage => $"Usage: {AppFileName} <path to {TargetFileName} update folder> <current {TargetFileName} process ID>";
        public static string AppUrl => "Have a look at https://github.com/mbodm/wowcam for more information";

        public static string TargetFolder = Helper.GetApplicationFolder();
        public static string TargetFileName = "WOWCAM.exe";
        public static string TargetFilePath = Path.Combine(TargetFolder, TargetFileName);

        public static void ShowError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(AppUrl);
        }

        public static string EvalUpdateFolderArg(string updateFolder)
        {
            try
            {
                return Path.GetFullPath(updateFolder);
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

        public static bool TargetApplicationRunning(int processId)
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
