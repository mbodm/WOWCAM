﻿using System.IO;
using System.Xml.Linq;

namespace WOWCAMUPD
{
    internal static class ConfigHelper
    {
        public static async Task<(bool Success, string Value)> GetUpdateFilePathFromConfigAsync(string targetFileName)
        {
            var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
            if (!File.Exists(xmlFile))
            {
                return (false, "Could not found WOWCAM config file.");
            }

            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
            var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
            if (string.IsNullOrWhiteSpace(temp))
            {
                return (false, "Could not determine WOWCAM temp folder.");
            }

            var tempFolder = Environment.ExpandEnvironmentVariables(temp);
            var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
            if (!Directory.Exists(updateFolder))
            {
                return (false, "Update folder not exists.");
            }

            fileStream.Close();

            return (true, Path.Combine(updateFolder, targetFileName));
        }
    }
}