using System.Collections.Concurrent;
using System.Xml.Linq;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultSmartUpdateFeature : ISmartUpdateFeature
    {
        private readonly ILogger logger;

        private readonly string rootFolder;
        private readonly string zipFolder;
        private readonly string xmlFile;

        private readonly ConcurrentDictionary<string, SmartUpdateData> dict = new();

        public DefaultSmartUpdateFeature(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            rootFolder = Path.Combine(AppHelper.GetApplicationExecutableFolder(), "SmartUpdate");
            zipFolder = Path.Combine(rootFolder, "LastDownloads");
            xmlFile = Path.Combine(rootFolder, "SmartUpdate.xml");
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            dict.Clear();

            if (!File.Exists(xmlFile)) return;

            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            var root = doc.Element("wowcam") ??
                throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");

            var parent = root.Element("smartupdate") ??
                throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            var entries = parent.Elements("entry");
            foreach (var entry in entries)
            {
                var addonName = entry?.Attribute("addonName")?.Value ?? string.Empty;
                var lastDownloadUrl = entry?.Attribute("lastDownloadUrl")?.Value ?? string.Empty;
                var lastDownloadFile = entry?.Attribute("lastDownloadFile")?.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(addonName) || string.IsNullOrWhiteSpace(lastDownloadUrl) || string.IsNullOrEmpty(lastDownloadFile))
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains one or more invalid entries.");

                dict.TryAdd(addonName, new SmartUpdateData(addonName, lastDownloadUrl, lastDownloadFile));
            }

            logger.LogMethodExit();
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            CreateFolderStructureIfNotExists();

            var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
                new XAttribute("addonName", kvp.Key),
                new XAttribute("lastDownloadUrl", kvp.Value.DownloadUrl),
                new XAttribute("lastZipFile", kvp.Value.ZipFile)));

            var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

            using var fileStream = new FileStream(xmlFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            await doc.SaveAsync(fileStream, SaveOptions.None, cancellationToken).ConfigureAwait(false);

            logger.LogMethodExit();
        }

        public bool AddonExists(string addonName, string downloadUrl, string zipFile)
        {
            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null) return false;

            var hasExactEntry = value.DownloadUrl == downloadUrl && value.ZipFile == zipFile;
            var zipFileExists = File.Exists(Path.Combine(zipFolder, zipFile));

            return hasExactEntry && zipFileExists;
        }

        public Task AddOrUpdateAddonAsync(string addonName, string downloadUrl, string zipFile, string downloadFolder, CancellationToken cancellationToken = default)
        {
            dict.AddOrUpdate(addonName, new SmartUpdateData(addonName, downloadUrl, zipFile), (_, _) => new SmartUpdateData(addonName, downloadUrl, zipFile));

            CreateFolderStructureIfNotExists();
            var sourcePath = Path.Combine(downloadFolder, zipFile);
            var destPath = Path.Combine(zipFolder, zipFile);
            File.Copy(sourcePath, destPath, true);

            return Task.Delay(250, cancellationToken);
        }

        public string GetZipFilePath(string addonName)
        {
            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
                throw new InvalidOperationException("SmartUpdate could not found an existing entry for given addon name.");

            var zipFilePath = Path.Combine(zipFolder, value.ZipFile);
            if (!File.Exists(zipFilePath))
                throw new InvalidProgramException("SmartUpdate could not found an existing zip file for given addon name.");

            return zipFilePath;
        }

        private void CreateFolderStructureIfNotExists()
        {
            if (!Directory.Exists(rootFolder)) Directory.CreateDirectory(rootFolder);
            if (!Directory.Exists(zipFolder)) Directory.CreateDirectory(zipFolder);
        }
    }
}
