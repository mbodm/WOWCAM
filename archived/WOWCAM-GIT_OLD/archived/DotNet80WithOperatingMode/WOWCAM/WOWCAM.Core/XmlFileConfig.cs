using System.Xml.Linq;

namespace WOWCAM.Core
{
    public sealed class XmlFileConfig(ILogger logger, ICurseHelper curseHelper) : IConfig
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        public string Storage => xmlFile;

        public string ActiveProfile { get; private set; } = string.Empty;
        public string TempFolder { get; private set; } = string.Empty;
        public OperatingMode OperatingMode { get; private set; } = OperatingMode.DownloadAndUnzip;
        public string DownloadFolder { get; private set; } = string.Empty;
        public string UnzipFolder { get; private set; } = string.Empty;
        public IEnumerable<string> AddonUrls { get; private set; } = Enumerable.Empty<string>();

        public bool Exists()
        {
            return File.Exists(xmlFile);
        }

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
                		<temp>%TEMP%</temp>
                	</general>
                	<profiles>
                		<retail>
                			<mode>DownloadAndUnzip</mode>
                			<folders>
                				<download>%USERPROFILE%\Desktop\RetailAddons</download>
                				<unzip>%PROGRAMFILES(X86)%\World of Warcraft\_retail_\Interface\AddOns</unzip>
                			</folders>
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
                var tail = "Please check the config file and visit the project's site for more information about the file format.";

                throw new InvalidOperationException($"{e.Message} {tail}");
            }
        }

        private void ReadData(XDocument doc)
        {
            CheckBasicFileStructure(doc);

            TempFolder = GetTempFolder(doc);
            ActiveProfile = GetActiveProfile(doc);

            CheckActiveProfileSection(doc, ActiveProfile);

            OperatingMode = GetOperatingMode(doc, ActiveProfile);

            switch (OperatingMode)
            {
                case OperatingMode.DownloadOnly:
                    DownloadFolder = GetDownloadFolder(doc, ActiveProfile);
                    AddonUrls = GetAddonUrls(doc, ActiveProfile);
                    break;
                case OperatingMode.UnzipOnly:
                    DownloadFolder = GetDownloadFolder(doc, ActiveProfile);
                    UnzipFolder = GetUnzipFolder(doc, ActiveProfile);
                    break;
                case OperatingMode.DownloadAndUnzip:
                    DownloadFolder = GetDownloadFolder(doc, ActiveProfile);
                    UnzipFolder = GetUnzipFolder(doc, ActiveProfile);
                    AddonUrls = GetAddonUrls(doc, ActiveProfile);
                    break;
                case OperatingMode.SmartUpdate:
                    // Todo
                    break;
            }
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

        private static string GetTempFolder(XDocument doc)
        {
            var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? string.Empty;

            // No <temp> not means error since <temp> is not a must-have setting

            return Environment.ExpandEnvironmentVariables(temp);
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

        private static void CheckActiveProfileSection(XDocument doc, string activeProfile)
        {
            if (doc.Root?.Element("profiles")?.Element(activeProfile) == null)
            {
                throw new InvalidOperationException("Error in config file: The active profile, specified in <general> section, not exists in <profiles> section.");
            }
        }

        private static OperatingMode GetOperatingMode(XDocument doc, string profile)
        {
            var mode = doc.Root?.Element("profiles")?.Element(profile)?.Element("mode")?.Value?.Trim() ?? string.Empty;

            if (mode == string.Empty || !Enum.TryParse(mode, out OperatingMode operatingMode))
            {
                throw new InvalidOperationException("Error in config file: Could not determine operating mode for given profile.");
            }

            return operatingMode;
        }

        private static string GetDownloadFolder(XDocument doc, string profile)
        {
            var download = doc.Root?.Element("profiles")?.Element(profile)?.Element("folders")?.Element("download")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine download folder for given profile.");

            return Environment.ExpandEnvironmentVariables(download);
        }

        private static string GetUnzipFolder(XDocument doc, string profile)
        {
            var unzip = doc.Root?.Element("profiles")?.Element(profile)?.Element("folders")?.Element("unzip")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine unzip folder for given profile.");

            return Environment.ExpandEnvironmentVariables(unzip);
        }

        private static IEnumerable<string> GetAddonUrls(XDocument doc, string profile)
        {
            var addons = doc.Root?.Element("profiles")?.Element(profile)?.Element("addons") ??
                throw new InvalidOperationException("Error in config file: Could not determine addons for given profile.");

            var urls = addons.Elements()?.Where(e => e.Name == "url")?.Select(e => e.Value.Trim().ToLower())?.Distinct() ?? [];

            return urls;
        }

        private void ValidateData()
        {
            const string NoDownloadFolder1 = "Config file contains no download folder to download the zip files into.";
            const string NoDownloadFolder2 = "Config file contains no download folder to unzip files from.";
            const string NoUnzipFolder = "Config file contains no unzip folder to extract the zip files into.";
            const string InvalidDownloadFolder = "Config file contains download folder which is not a valid absolute file system path.";
            const string InvalidUnzipFolder = "Config file contains unzip folder which is not a valid absolute file system path.";
            const string NoAddonUrls = "Config file contains 0 url entries and this means there is nothing to download.";
            const string InvalidAddonUrls = "Config file contains at least 1 url entry which is not a valid Curse addon url.";

            switch (OperatingMode)
            {
                case OperatingMode.DownloadOnly:
                    if (DownloadFolder == string.Empty) throw new InvalidOperationException(NoDownloadFolder1);
                    if (!IsValidAbsolutePath(DownloadFolder)) throw new InvalidOperationException(InvalidDownloadFolder);
                    break;
                case OperatingMode.UnzipOnly:
                    if (DownloadFolder == string.Empty) throw new InvalidOperationException(NoDownloadFolder2);
                    if (!IsValidAbsolutePath(DownloadFolder)) throw new InvalidOperationException(InvalidDownloadFolder);
                    if (UnzipFolder == string.Empty) throw new InvalidOperationException(NoUnzipFolder);
                    if (!IsValidAbsolutePath(UnzipFolder)) throw new InvalidOperationException(InvalidUnzipFolder);
                    break;
                case OperatingMode.DownloadAndUnzip:
                    if (DownloadFolder == string.Empty) throw new InvalidOperationException(NoDownloadFolder1);
                    if (!IsValidAbsolutePath(DownloadFolder)) throw new InvalidOperationException(InvalidDownloadFolder);
                    if (UnzipFolder == string.Empty) throw new InvalidOperationException(NoUnzipFolder);
                    if (!IsValidAbsolutePath(UnzipFolder)) throw new InvalidOperationException(InvalidUnzipFolder);
                    if (!AddonUrls.Any()) throw new InvalidOperationException(NoAddonUrls);
                    if (AddonUrls.Any(url => !curseHelper.IsAddonPageUrl(url))) throw new InvalidOperationException(InvalidAddonUrls);
                    break;
                case OperatingMode.SmartUpdate:
                    // Todo
                    break;
            }
        }

        private static bool IsValidAbsolutePath(string path)
        {
            try
            {
                Path.GetFullPath(path);
            }
            catch
            {
                return false;
            }

            try
            {
                return Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
