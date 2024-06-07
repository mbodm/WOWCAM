using System.Xml.Linq;

namespace wcupdate
{
    internal static class Core
    {
        public static string TargetAppName => "WOWCAM";
        public static string TargetFileName => "WOWCAM.exe";

        public static void ShowError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Help);
        }

        public static async Task<string> GetUpdateFolderAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
                using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
                var tempFolder = Environment.ExpandEnvironmentVariables(temp);
                var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");

                return updateFolder;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
