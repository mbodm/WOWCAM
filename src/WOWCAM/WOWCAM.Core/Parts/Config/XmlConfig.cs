using System.Xml.Linq;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

// Todo: Do consistent BL exception handling here.

namespace WOWCAM.Core.Parts.Config
{
    public sealed class XmlConfig(ILogger logger) : IConfig
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        public ConfigData Data { get; private set; } = new ConfigData(string.Empty, string.Empty, [], string.Empty, []);

        public string Storage => xmlFile;
        public bool StorageExists => File.Exists(xmlFile);

        public Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default)
        {
            var s = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- ===================================================================== -->
                <!-- Please have a look at https://github.com/mbodm/wowcam for file format -->
                <!-- ===================================================================== -->
                <wowcam>
                	<general>
                		<profile>retail</profile>
                		<temp>%TEMP%</temp>
                	</general>
                	<options>
                		<autoupdate>false</autoupdate>
                		<silentmode>false</silentmode>
                	</options>
                	<profiles>
                		<retail>
                			<folder>%PROGRAMFILES(X86)%\World of Warcraft\_retail_\Interface\AddOns</folder>
                			<addons>
                				<url>https://www.curseforge.com/wow/addons/deadly-boss-mods</url>
                				<url>https://www.curseforge.com/wow/addons/details</url>
                				<url>https://www.curseforge.com/wow/addons/weakauras-2</url>
                			</addons>
                		</retail>
                	</profiles>
                </wowcam>
                """;

            s += Environment.NewLine;

            var configFolder = Path.GetDirectoryName(xmlFile) ?? string.Empty;
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }

            return File.WriteAllTextAsync(xmlFile, s, cancellationToken);
        }

        public async Task LoadFromStorageAsync(CancellationToken cancellationToken = default)
        {
            XDocument doc;

            try
            {
                using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not load config file (see log file for details).", e);
            }

            try
            {
                CheckBasicFileStructure(doc);

                var activeProfile = GetActiveProfile(doc);
                CheckActiveProfileSection(doc, activeProfile);
                var tempFolder = GetTempFolder(doc);
                var activeOptions = GetActiveOptions(doc);

                var targetFolder = GetTargetFolder(doc, activeProfile);
                var addonUrls = GetAddonUrls(doc, activeProfile);

                Data = new ConfigData(activeProfile, tempFolder, activeOptions, targetFolder, addonUrls);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Format error in config file (see log file for details).", e);
            }
        }

        public void Validate()
        {
            // See details and reasons for MaxPathLength value at:
            // https://stackoverflow.com/questions/265769/maximum-filename-length-in-ntfs-windows-xp-and-windows-vista
            // https://stackoverflow.com/questions/23588944/better-to-check-if-length-exceeds-max-path-or-catch-pathtoolongexception

            const int MaxPathLength = 240;

            try
            {
                if (Data.TempFolder == string.Empty)
                    throw new InvalidOperationException("Config file contains no temp folder and also the application's own default fallback value (%TEMP%) is not active.");

                ValidateFolder(Data.TempFolder, "temp", MaxPathLength);

                if (Data.TargetFolder == string.Empty)
                    throw new InvalidOperationException("Config file contains no target folder to download and extract the zip files into.");

                // Easy to foresee max length of temp. Not that easy to foresee max length of target, when considering content of
                // zip file (files and subfolders). Therefore just using half of MAX_PATH here, as some "rule of thumb". If in a
                // rare case a full dest path exceeds MAX_PATH, it seems OK to let the unzip operation fail gracefully on its own.

                ValidateFolder(Data.TargetFolder, "target", MaxPathLength / 2);

                if (!Data.AddonUrls.Any())
                    throw new InvalidOperationException("Config file contains 0 addon URL entries and so there is nothing to download.");

                if (Data.AddonUrls.Any(url => !CurseHelper.IsAddonPageUrl(url)))
                    throw new InvalidOperationException("Config file contains at least 1 addon URL entry which is not a valid Curse addon URL.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw;
            }
        }

        private static void CheckBasicFileStructure(XDocument doc)
        {
            var root = doc.Root;

            if (root == null || root.Name != "wowcam")
                throw new InvalidOperationException("Error in config file: The <wowcam> root element not exists.");

            if (root.Element("general") == null)
                throw new InvalidOperationException("Error in config file: The <general> section not exists.");

            if (root.Element("options") == null)
                throw new InvalidOperationException("Error in config file: The <options> section not exists.");

            var profiles = root.Element("profiles") ??
                throw new InvalidOperationException("Error in config file: The <profiles> section not exists.");

            if (!profiles.HasElements)
                throw new InvalidOperationException("Error in config file: The <profiles> section not contains any profiles.");
        }

        private static string GetActiveProfile(XDocument doc)
        {
            return doc.Root?.Element("general")?.Element("profile")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine active profile.");
        }

        private static void CheckActiveProfileSection(XDocument doc, string activeProfile)
        {
            if (doc.Root?.Element("profiles")?.Element(activeProfile) == null)
                throw new InvalidOperationException("Error in config file: The active profile, specified in <general> section, not exists in <profiles> section.");
        }

        private static string GetTempFolder(XDocument doc)
        {
            // No <temp> not means error since it's not a must-have setting (if not existing a fallback value is used)
            var s = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";

            return Environment.ExpandEnvironmentVariables(s);
        }

        private static IEnumerable<string> GetActiveOptions(XDocument doc)
        {
            var options = doc.Root?.Element("options") ??
                throw new InvalidOperationException("Error in config file: Could not determine options.");

            List<string> result = [];
            foreach (var option in options.Elements())
            {
                var value = option.Value.ToString().Trim().ToLower();
                switch (value)
                {
                    case "":
                        throw new InvalidOperationException("Error in config file: Found <options> entry with empty or whitespace value (supported values are 'true' and 'false').");
                    case "true":
                        result.Add(option.Name.ToString().Trim().ToLower());
                        break;
                    case "false":
                        // Do nothing (just not add option to result)
                        break;
                    default:
                        throw new InvalidOperationException("Error in config file: Found <options> entry with unsupported value (supported values are 'true' and 'false').");
                }
            }

            return result.AsEnumerable();
        }

        private static string GetTargetFolder(XDocument doc, string profile)
        {
            var s = doc.Root?.Element("profiles")?.Element(profile)?.Element("folder")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine target folder for given profile.");

            return Environment.ExpandEnvironmentVariables(s);
        }

        private static IEnumerable<string> GetAddonUrls(XDocument doc, string profile)
        {
            var addons = doc.Root?.Element("profiles")?.Element(profile)?.Element("addons") ??
                throw new InvalidOperationException("Error in config file: Could not determine addon urls for given profile.");

            return addons.Elements()?.Where(e => e.Name == "url")?.Select(e => e.Value.Trim().ToLower())?.Distinct() ?? [];
        }

        private static void ValidateFolder(string folderValue, string folderName, int maxChars)
        {
            if (!FileSystemHelper.IsValidAbsolutePath(folderValue))
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder which is not a valid folder path (given path must be a valid absolute path to a folder).");

            if (folderValue.Length > maxChars)
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder path which is too long (make sure given path is smaller than {maxChars} characters).");

            // I decided to NOT create any configured folder by code since the default config makes various assumptions i.e. about WoW folder in %PROGRAMFILES(X86)%

            if (!Directory.Exists(folderValue))
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder which not exists (the app will not create any configured folder automatically, on purpose).");
        }
    }
}
