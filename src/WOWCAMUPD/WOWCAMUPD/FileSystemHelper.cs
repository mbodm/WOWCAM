using System.Diagnostics;
using System.IO;

namespace WOWCAMUPD
{
    internal static class FileSystemHelper
    {
        public static bool IsValidAbsolutePath(string path)
        {
            try
            {
                Path.GetFullPath(path);

                return Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }

        public static bool CopyFile(string sourceFilePath, string destFilePath)
        {
            // Try to copy file as user, otherwise ask for admin rights and copy file as admin, if user has no write access to folder.

            try
            {
                File.Copy(sourceFilePath, destFilePath, true);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C copy /B /V /Y {sourceFilePath} {destFilePath}",
                    CreateNoWindow = true,
                    Verb = "runas" // Ask for admin rights
                };

                return Process.Start(psi) != null;
            }
        }
    }
}
