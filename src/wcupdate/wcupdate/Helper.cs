using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace wcupdate
{
    internal static class Helper
    {
        public static string GetAssemblyName() =>
            Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";

        // It's the counterpart of the "Version" entry, declared in the .csproj file.
        public static string GetApplicationVersion() =>
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        public static bool ApplicationHasAdminRights()
        {
            // See StackOverflow:
            // https://stackoverflow.com/questions/5953240/check-for-administrator-privileges-in-c-sharp
            // https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator/11660205

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var identity = WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }

            return false;
        }

        public static bool ProcessIsRunning(int processId)
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

        public static bool CloseProcess(int processId)
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

        public static bool OverwriteFile(string source, string dest)
        {
            try
            {
                File.Copy(source, dest, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string filePath)
        {
            try
            {
                return Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-Command Start-Process \"{filePath}\"" }) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
