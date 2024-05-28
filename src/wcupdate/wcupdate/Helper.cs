using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace wcupdate
{
    internal static class Helper
    {
        public static string GetAssemblyName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";
        }

        public static string GetApplicationVersion()
        {
            // It's the counterpart of the "Version" entry, declared in the .csproj file.

            return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        }

        public static string GetApplicationFolder()
        {
            return Path.GetFullPath(AppContext.BaseDirectory);
        }

        public static Version? GetExeFileVersion(string pathToExeFile)
        {
            try
            {
                var exeFile = Path.GetFullPath(pathToExeFile);

                if (!File.Exists(exeFile) || Path.GetExtension(exeFile) != ".exe")
                {
                    return null;
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(exeFile);
                var productVersion = fileVersionInfo.ProductVersion;

                return productVersion != null ? new Version(productVersion) : null;
            }
            catch
            {
                return null;
            }
        }

        public static bool ApplicationHasAdminRights()
        {
            // See StackOverflow:
            // https://stackoverflow.com/questions/5953240/check-for-administrator-privileges-in-c-sharp
            // https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator/11660205

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    if (identity != null)
                    {
                        var principal = new WindowsPrincipal(identity);

                        return principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
            }

            return false;
        }
    }
}
