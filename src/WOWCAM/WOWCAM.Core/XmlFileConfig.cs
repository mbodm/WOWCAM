using System.Xml.Linq;

namespace WOWCAM.Core
{
    public sealed class XmlFileConfig(ILogger logger, ICurseHelper curseHelper) : IConfig
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        public string ActiveProfile { get; private set; } = string.Empty;
        public string ApplicationMode { get; private set; } = string.Empty;
        public string TempFolder { get; private set; } = string.Empty;
        public string TargetFolder { get; private set; } = string.Empty;
        public IEnumerable<string> AddonUrls { get; private set; } = [];

        public string Storage => xmlFile;
        public bool Exists => File.Exists(xmlFile);

        public Task CreateEmptyAsync(CancellationToken cancellationToken = default)
        {
            var s = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- ===================================================================== -->
                <!-- Please have a look at https://github.com/mbodm/wowcam for file format -->
                <!-- ===================================================================== -->
                <wowcam>
                	<general>
                		<profile>retail</profile>
                		<mode>normal</mode>
                		<temp>%TEMP%</temp>
                	</general>
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

            return File.WriteAllTextAsync(xmlFile, s, cancellationToken);
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            XDocument doc;

            try
            {
                using var fileStream = new FileStream(Storage, FileMode.Open, FileAccess.Read, FileShare.Read);

                doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw new InvalidOperationException("Could not load config file (see log file for details).", e);
            }

            try
            {
                ReadData(doc);
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw new InvalidOperationException("Format error in config file (see log file for details).", e);
            }

            try
            {
                ValidateData();
            }
            catch (Exception e)
            {
                var tail = "Please check the config file and visit the project's site for more information about the config file format.";

                throw new InvalidOperationException($"{e.Message} {tail}");
            }
        }

        private void ReadData(XDocument doc)
        {
            CheckBasicFileStructure(doc);

            ActiveProfile = GetActiveProfile(doc);
            ApplicationMode = GetApplicationMode(doc);
            TempFolder = GetTempFolder(doc);

            CheckActiveProfileSection(doc, ActiveProfile);

            TargetFolder = GetTargetFolder(doc, ActiveProfile);
            AddonUrls = GetAddonUrls(doc, ActiveProfile);
        }

        private static void CheckBasicFileStructure(XDocument doc)
        {
            var root = doc.Root;

            if (root == null || root.Name != "wowcam")
                throw new InvalidOperationException("Error in config file: The <wowcam> root element not exists.");

            var general = root.Element("general") ??
                throw new InvalidOperationException("Error in config file: The <general> section not exists.");

            if (!general.HasElements)
                throw new InvalidOperationException("Error in config file: The <general> section not contains any elements.");

            var profiles = root.Element("profiles") ??
                throw new InvalidOperationException("Error in config file: The <profiles> section not exists.");

            if (!profiles.HasElements)
                throw new InvalidOperationException("Error in config file: The <profiles> section not contains any elements.");
        }

        private static string GetActiveProfile(XDocument doc)
        {
            var profile = doc.Root?.Element("general")?.Element("profile")?.Value?.Trim() ?? string.Empty;

            if (profile == string.Empty)
            {
                throw new InvalidOperationException("Error in config file: Could not determine active profile.");
            }

            return profile;
        }

        private static string GetApplicationMode(XDocument doc)
        {
            var mode = doc.Root?.Element("general")?.Element("mode")?.Value?.Trim() ?? string.Empty;

            // No <mode> not means error since <mode> is not a must-have setting

            return mode;
        }

        private static string GetTempFolder(XDocument doc)
        {
            var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? string.Empty;

            // No <temp> not means error since <temp> is not a must-have setting

            return Environment.ExpandEnvironmentVariables(temp);
        }

        private static void CheckActiveProfileSection(XDocument doc, string activeProfile)
        {
            if (doc.Root?.Element("profiles")?.Element(activeProfile) == null)
            {
                throw new InvalidOperationException("Error in config file: The active profile, specified in <general> section, not exists in <profiles> section.");
            }
        }

        private static string GetTargetFolder(XDocument doc, string profile)
        {
            var folder = doc.Root?.Element("profiles")?.Element(profile)?.Element("folders")?.Element("download")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine target folder for given profile.");

            return Environment.ExpandEnvironmentVariables(folder);
        }

        private static IEnumerable<string> GetAddonUrls(XDocument doc, string profile)
        {
            var addons = doc.Root?.Element("profiles")?.Element(profile)?.Element("addons") ??
                throw new InvalidOperationException("Error in config file: Could not determine addon urls for given profile.");

            var urls = addons.Elements()?.Where(e => e.Name == "url")?.Select(e => e.Value.Trim().ToLower())?.Distinct() ?? [];

            return urls;
        }

        private void ValidateData()
        {
            if (TargetFolder == string.Empty)
            {
                throw new InvalidOperationException("Config file contains no target folder to download and extract the zip files into.");
            }

            if (!IsValidAbsolutePath(TargetFolder))
            {
                throw new InvalidOperationException("Config file contains a target folder which is not a valid absolute file system path.");
            }

            if (!AddonUrls.Any())
            {
                throw new InvalidOperationException("Config file contains 0 addon url entries and this means there is nothing to download.");
            }

            if (AddonUrls.Any(url => !curseHelper.IsAddonPageUrl(url)))
            {
                throw new InvalidOperationException("Config file contains at least 1 url entry which is not a valid Curse addon url.");
            }
        }

        private static bool IsValidAbsolutePath(string path)
        {
            try
            {
                if (!Path.IsPathRooted(path))
                {
                    return false;
                }

                Path.GetFullPath(path);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
