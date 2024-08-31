using System.Xml.Linq;

namespace wcupdate
{
    internal static class Core
    {
        public const string TargetAppName = "WOWCAM";
        public const string TargetFileName = "WOWCAM.exe";

        public static void Process(Func<bool> successPredicate, int exitCode, string errorMessage, string statusMessage)
        {
            if (!successPredicate())
            {
                Console.WriteLine($"Error: {errorMessage}");
                Console.WriteLine();
                Console.WriteLine(App.Help);
                Environment.Exit(exitCode);
            }
            else
            {
                Console.WriteLine($" - {statusMessage}");
            }
        }

        public static async Task<string> GetUpdateFilePathAsync(bool verbose, CancellationToken cancellationToken = default)
        {
            try
            {
                var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
                if (verbose) Console.WriteLine($"Used config file: {xmlFile}");
                using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (verbose) Console.WriteLine("File stream opened");
                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                if (verbose) Console.WriteLine("XML document loaded");
                var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
                var tempFolder = Environment.ExpandEnvironmentVariables(temp);
                if (verbose) Console.WriteLine($"Determined temp folder: {tempFolder}");
                var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
                if (verbose) Console.WriteLine($"Determined update folder: {updateFolder}");
                var updateFile = Path.Combine(updateFolder, TargetFileName);
                if (verbose) Console.WriteLine($"Determined update file: {updateFile}");

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
