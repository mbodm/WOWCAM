using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WCUPDATE
{
    internal static class Helper
    {
        public static string GetAssemblyName()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Name ?? "UNKNOWN";
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

        public static Version GetExeFileVersion(string pathToExeFile)
        {
            var errorResult = new Version(0, 0, 0);

            try
            {
                var exeFile = Path.GetFullPath(pathToExeFile);

                if (!File.Exists(exeFile) || Path.GetExtension(exeFile) != ".exe")
                {
                    return errorResult;
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(exeFile);
                var productVersion = fileVersionInfo.ProductVersion;

                if (productVersion == null)
                {
                    return errorResult;
                }

                return new Version(productVersion);
            }
            catch
            {
                return errorResult;
            }
        }
    }
}
