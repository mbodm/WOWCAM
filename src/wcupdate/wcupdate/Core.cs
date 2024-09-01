using System.Xml.Linq;

namespace wcupdate
{
    internal static class Core
    {
        public const string TargetAppName = "WOWCAM";
        public const string TargetFileName = "WOWCAM.exe";

        public static void ShowStatus(string statusMessage)
        {
            Console.WriteLine($" - {statusMessage}");
        }

        public static void ShowErrorAndExit(string errorMessage, int exitCode)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Help);

            Environment.Exit(exitCode);
        }

        public static void Eval(Func<bool> predicate, string statusMessage, string errorMessage, int exitCode)
        {
            if (predicate())
            {
                ShowStatus(statusMessage);
            }
            else
            {
                Console.WriteLine();
                ShowErrorAndExit(errorMessage, exitCode);
            }
        }

        public static async Task<string> GetUpdateFilePathAsync(bool verbose, CancellationToken cancellationToken = default)
        {
            try
            {
                var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
                if (verbose) ShowStatus($"Config -> Used config file: {xmlFile}");

                using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (verbose) ShowStatus("Config -> File stream opened");

                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                if (verbose) ShowStatus("Config -> XML document loaded");

                var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
                var tempFolder = Environment.ExpandEnvironmentVariables(temp);
                if (verbose) ShowStatus($"Config -> Temp folder: {tempFolder}");

                var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
                if (verbose) ShowStatus($"Config -> Update folder: {updateFolder}");

                var updateFile = Path.Combine(updateFolder, TargetFileName);
                if (verbose) ShowStatus($"Config -> Update file: {updateFile}");

                return updateFile;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return string.Empty;
            }
        }

        public static string GetTargetFilePath()
        {
            return Path.Combine(Helper.GetApplicationExecutableFolder(), TargetFileName);
        }
    }
}
