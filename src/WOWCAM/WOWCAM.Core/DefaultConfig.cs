﻿using System.Xml.Linq;

namespace WOWCAM.Core
{
    public sealed class DefaultConfig(ILogger logger) : IConfig
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        public string ActiveProfile { get; private set; } = string.Empty;
        public string TempFolder { get; private set; } = string.Empty;
        public string TargetFolder { get; private set; } = string.Empty;
        public IEnumerable<string> AddonUrls { get; private set; } = [];
        public IEnumerable<string> Options { get; private set; } = [];

        public string Storage => xmlFile;
        public bool Exists => File.Exists(xmlFile);

        public Task CreateDefaultAsync(CancellationToken cancellationToken = default)
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
                		<smartupdate>false</smartupdate>
                		<silentmode>false</silentmode>
                		<unziponly>false</unziponly>
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
                CheckBasicFileStructure(doc);

                ActiveProfile = GetActiveProfile(doc);
                CheckActiveProfileSection(doc, ActiveProfile);
                TempFolder = GetTempFolder(doc);

                TargetFolder = GetTargetFolder(doc, ActiveProfile);
                AddonUrls = GetAddonUrls(doc, ActiveProfile);

                Options = GetOptions(doc);
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw new InvalidOperationException("Format error in config file (see log file for details).", e);
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

        private static IEnumerable<string> GetOptions(XDocument doc)
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
    }
}
