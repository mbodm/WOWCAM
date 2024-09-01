﻿using System.Diagnostics;
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

        public static string GetApplicationExecutableFolder()
        {
            return Path.GetFullPath(AppContext.BaseDirectory);
        }

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

        public static bool ProcessIsRunning(string exeFileName)
        {
            try
            {
                var p = Process.GetProcessesByName("wowcam");
                if (p.Length == 0)
                {
                    Console.WriteLine($"Fuzz: Keine Prozess (exeFileName war {exeFileName}");
                    return false;
                }
                else
                {
                    Console.WriteLine($"Fuzz: Prozess ID war {p.First().Id}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static bool KillProcess(string exeFileName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = $"/F /IM {exeFileName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                return Process.Start(startInfo) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string exeFilePath)
        {
            try
            {
                return Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/C \"{exeFilePath}\"" }) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool OverwriteFile(string sourceFilePath, string destFilePath)
        {
            try
            {
                File.Copy(sourceFilePath, destFilePath, true);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
