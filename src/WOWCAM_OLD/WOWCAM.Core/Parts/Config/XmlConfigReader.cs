using System.Xml.Linq;
using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Config
{
    public sealed class XmlConfigReader(ILogger logger, IConfigStorage storage) : IConfigReader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigStorage storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public async Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            var xmlFile = storage.StorageInformation;
            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            CheckBasicFileStructure(doc);

            var activeProfile = GetActiveProfile(doc);
            CheckActiveProfileSection(doc, activeProfile);

            var result = new ConfigData(
                ActiveProfile: activeProfile,
                Theme: GetTheme(doc),
                TempFolder: GetTempFolder(doc),
                ActiveOptions: GetActiveOptions(doc),
                TargetFolder: GetTargetFolder(doc, activeProfile),
                AddonUrls: GetAddonUrls(doc, activeProfile));

            logger.LogMethodExit();

            return result;
        }

        private static void CheckBasicFileStructure(XDocument doc)
        {
            var root = doc.Root;

            if (root == null || root.Name != "wowcam")
            {
                throw new InvalidOperationException("Error in config file: The <wowcam> root element not exists.");
            }

            if (root.Element("general") == null)
            {
                throw new InvalidOperationException("Error in config file: The <general> section not exists.");
            }

            if (root.Element("options") == null)
            {
                throw new InvalidOperationException("Error in config file: The <options> section not exists.");
            }

            var profiles = root.Element("profiles") ?? throw new InvalidOperationException("Error in config file: The <profiles> section not exists.");
            if (!profiles.HasElements)
            {
                throw new InvalidOperationException("Error in config file: The <profiles> section not contains any profiles.");
            }
        }

        private static string GetActiveProfile(XDocument doc)
        {
            var value = doc.Root?.Element("general")?.Element("profile")?.Value?.Trim();
            var result = value ?? throw new InvalidOperationException("Error in config file: Could not determine active profile.");

            return result;
        }

        private static void CheckActiveProfileSection(XDocument doc, string activeProfile)
        {
            if (doc.Root?.Element("profiles")?.Element(activeProfile) == null)
            {
                throw new InvalidOperationException("Error in config file: The active profile, specified in <general> section, not exists in <profiles> section.");
            }
        }

        private static string GetTheme(XDocument doc)
        {
            // No <theme> not means error since it's not a must-have setting (a fallback value is used if <theme> not exists)
            var result = doc.Root?.Element("general")?.Element("theme")?.Value?.Trim() ?? "system";

            return result;
        }

        private static string GetTempFolder(XDocument doc)
        {
            // No <temp> not means error since it's not a must-have setting (a fallback value is used if <temp> not exists)
            var value = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
            var result = Environment.ExpandEnvironmentVariables(value);

            return result;
        }

        private static IEnumerable<string> GetActiveOptions(XDocument doc)
        {
            var options = doc.Root?.Element("options") ?? throw new InvalidOperationException("Error in config file: Could not determine options.");

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
            var value = doc.Root?.Element("profiles")?.Element(profile)?.Element("folder")?.Value?.Trim() ??
                throw new InvalidOperationException("Error in config file: Could not determine target folder for given profile.");

            var result = Environment.ExpandEnvironmentVariables(value);

            return result;
        }

        private static IEnumerable<string> GetAddonUrls(XDocument doc, string profile)
        {
            var addons = doc.Root?.Element("profiles")?.Element(profile)?.Element("addons") ??
                throw new InvalidOperationException("Error in config file: Could not determine addon urls for given profile.");

            var result = addons.Elements()?.Where(e => e.Name == "url")?.Select(e => e.Value.Trim().ToLower())?.Distinct() ?? [];

            return result;
        }
    }
}
