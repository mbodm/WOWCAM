using System.Diagnostics;
using System.Xml.Linq;

namespace wcupdate
{
    internal static class Core
    {
        public const string TargetAppName = "WOWCAM";
        public const string TargetFileName = "WOWCAM.exe";

        public static void Step1CheckRights(string statusMessage, string errorMessage, int exitCode)
        {
            bool hasAdminRights = false;
            try
            {
                hasAdminRights = Helper.ApplicationHasAdminRights();
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            if (hasAdminRights)
            {
                Flow.Status(statusMessage, "Rights");
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }

        public static async Task<string> Step2DetermineUpdateFileAsync(string targetAppConfigFile, string statusMessage, string errorMessage, int exitCode)
        {
            string updateFilePath = string.Empty;
            try
            {
                //var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
                var xmlFile = targetAppConfigFile;
                Flow.Status("Defined config file", "Config", xmlFile);

                using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                Flow.Status("Opened file stream", "Config");

                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
                Flow.Status("Loaded XML document", "Config");

                var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
                var tempFolder = Environment.ExpandEnvironmentVariables(temp);
                Flow.Status("Read temp folder", "Config", tempFolder);

                var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
                Flow.Status("Defined update folder", "Config", updateFolder);

                fileStream.Close();
                Flow.Status("Closed file stream", "Config");

                updateFilePath = Path.Combine(updateFolder, TargetFileName);
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            if (!string.IsNullOrWhiteSpace(updateFilePath))
            {
                Flow.Status(statusMessage, "Files", updateFilePath);
                return updateFilePath;
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
                return string.Empty;
            }
        }

        public static void Step3CheckUpdateFile(string updateFilePath, string statusMessage, string errorMessage, int exitCode)
        {
            if (File.Exists(updateFilePath))
            {
                Flow.Status(statusMessage, "Files");
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }

        public static string Step4DetermineTargetFile(string statusMessage, string errorMessage, int exitCode)
        {
            var targetFilePath = Path.Combine(Helper.GetApplicationExecutableFolder(), TargetFileName);
            if (!string.IsNullOrWhiteSpace(targetFilePath))
            {
                Flow.Status(statusMessage, "Files", targetFilePath);
                return targetFilePath;
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
                return string.Empty; // Needed (compiler cant't know)
            }
        }

        public static void Step5CheckTargetFile(string targetFilePath, string statusMessage, string errorMessage, int exitCode)
        {
            if (File.Exists(targetFilePath))
            {
                Flow.Status(statusMessage, "Files");
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }

        public static void Step6CheckProcess(string statusMessage, string errorMessage, int exitCode)
        {
            Process[]? processes = null;
            try
            {
                processes = Process.GetProcessesByName(TargetAppName);
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            if (processes != null && processes.Length > 0 && processes.First().ProcessName == TargetAppName)
            {
                Flow.Status(statusMessage, "Proc");
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }

        public static async Task Step7KillProcessAsync(string statusMessage, string errorMessage, int exitCode)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/F /IM {TargetFileName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process? process = null;
            try
            {
                process = Process.Start(psi);
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            if (process != null)
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
                if (process.ExitCode == 0)
                {
                    Flow.Status(statusMessage, "Proc");
                }
                else
                {
                    Flow.Exit(errorMessage, exitCode);
                }
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }

        public static void Step8CopyFile(string updateFilePath, string targetFilePath, string statusMessage, string errorMessage, int exitCode)
        {
            try
            {
                File.Copy(updateFilePath, targetFilePath, true);
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            Flow.Status(statusMessage, "Copy");
        }

        public static void Step9StartApp(string targetFilePath, string statusMessage, string errorMessage, int exitCode)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{targetFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process? process = null;
            try
            {
                process = Process.Start(psi);
            }
            catch (Exception e)
            {
                Flow.Exit(errorMessage, exitCode, e);
            }

            if (process != null)
            {
                Flow.Status(statusMessage, "Proc");
            }
            else
            {
                Flow.Exit(errorMessage, exitCode);
            }
        }
    }
}
