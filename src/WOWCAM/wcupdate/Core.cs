﻿using System.Xml.Linq;

namespace wcupdate
{
    internal static class Core
    {
        public static void Print(string msg)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        }

        public static void PrintError(string msg)
        {
            Print($"Error: {msg}");
            Print("Stopped");
        }

        public static void PrintException(Exception e)
        {
            Print("Error: An exception occurred.");
            Print($"-> Exception-Type: {e.GetType()}");
            Print($"-> Exception-Message: {e.Message}");
            Print("Stopped");
        }

        public static bool Step(Func<bool> predicate, string successMessage, string errorMessage)
        {
            var result = predicate();

            if (result) Print(successMessage);
            else PrintError(errorMessage);

            return result;
        }

        public static async Task<(bool Success, string Value, string Error)> GetUpdateFolderFromConfigAsync()
        {
            var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
            if (!File.Exists(xmlFile))
            {
                return (false, string.Empty, "Could not found WOWCAM config file.");
            }

            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
            var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
            if (string.IsNullOrWhiteSpace(temp))
            {
                return (false, string.Empty, "Could not determine WOWCAM temp folder.");
            }

            var tempFolder = Environment.ExpandEnvironmentVariables(temp);
            var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
            if (!Directory.Exists(updateFolder))
            {
                return (false, string.Empty, "Update folder not exists.");
            }

            fileStream.Close();

            return (true, updateFolder, string.Empty);
        }

    }
}