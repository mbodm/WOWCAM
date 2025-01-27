using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultSmartUpdateFeature(ILogger logger, IConfigModule configModule) : ISmartUpdateFeature
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigModule configModule = configModule ?? throw new ArgumentNullException(nameof(configModule));

        private readonly ConcurrentDictionary<string, SmartUpdateData> dict = new();

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            dict.Clear();

            var xmlFile = GetXmlFilePath();
            if (!File.Exists(xmlFile))
            {
                return;
            }

            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            XDocument doc;
            try
            {
                doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while loading SmartUpdate file: The file is either empty or not a valid XML file.");
            }

            var root = doc.Element("wowcam") ?? throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");
            var parent = root.Element("smartupdate") ?? throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            var entries = parent.Elements("entry");
            foreach (var entry in entries)
            {
                var addonName = entry?.Attribute("addonName")?.Value ?? string.Empty;
                var lastDownloadUrl = entry?.Attribute("lastDownloadUrl")?.Value ?? string.Empty;
                var lastDownloadFile = entry?.Attribute("lastZipFile")?.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(addonName) || string.IsNullOrWhiteSpace(lastDownloadUrl) || string.IsNullOrEmpty(lastDownloadFile))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains one or more invalid entries.");
                }

                dict.TryAdd(addonName, new SmartUpdateData(addonName, lastDownloadUrl, lastDownloadFile));
            }

            logger.LogMethodExit();
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
                new XAttribute("addonName", kvp.Key),
                new XAttribute("lastDownloadUrl", kvp.Value.DownloadUrl),
                new XAttribute("lastZipFile", kvp.Value.ZipFile)));

            var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

            CreateFolderStructureIfNotExists();

            var xmlFile = GetXmlFilePath();
            using var fileStream = new FileStream(xmlFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true, IndentChars = "\t", NewLineOnAttributes = true, Async = true });
            await xmlWriter.FlushAsync().ConfigureAwait(false);
            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            await doc.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);

            logger.LogMethodExit();
        }

        public bool AddonExists(string addonName, string downloadUrl, string zipFile)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            if (string.IsNullOrWhiteSpace(zipFile))
            {
                throw new ArgumentException($"'{nameof(zipFile)}' cannot be null or whitespace.", nameof(zipFile));
            }

            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
            {
                return false;
            }

            var hasExactEntry = value.DownloadUrl == downloadUrl && value.ZipFile == zipFile;
            var zipFileExists = File.Exists(Path.Combine(GetZipFolderPath(), zipFile));

            return hasExactEntry && zipFileExists;
        }

        public Task AddOrUpdateAddonAsync(string addonName, string downloadUrl, string zipFile, string downloadFolder, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            if (string.IsNullOrWhiteSpace(zipFile))
            {
                throw new ArgumentException($"'{nameof(zipFile)}' cannot be null or whitespace.", nameof(zipFile));
            }

            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                throw new ArgumentException($"'{nameof(downloadFolder)}' cannot be null or whitespace.", nameof(downloadFolder));
            }

            dict.AddOrUpdate(addonName, new SmartUpdateData(addonName, downloadUrl, zipFile), (_, _) => new SmartUpdateData(addonName, downloadUrl, zipFile));

            CreateFolderStructureIfNotExists();
            var sourcePath = Path.Combine(downloadFolder, zipFile);
            var destPath = Path.Combine(GetZipFolderPath(), zipFile);
            File.Copy(sourcePath, destPath, true);

            return Task.Delay(100, cancellationToken);
        }

        public string GetZipFilePath(string addonName)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
            {
                throw new InvalidOperationException("SmartUpdate could not found an existing entry for given addon name.");
            }

            var zipFilePath = Path.Combine(GetZipFolderPath(), value.ZipFile);
            if (!File.Exists(zipFilePath))
            {
                throw new InvalidProgramException("SmartUpdate could not found an existing zip file for given addon name.");
            }

            return zipFilePath;
        }

        private void CreateFolderStructureIfNotExists()
        {
            var rootFolder = GetRootFolderPath();
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            var zipFolder = GetZipFolderPath();
            if (!Directory.Exists(zipFolder))
            {
                Directory.CreateDirectory(zipFolder);
            }
        }

        private string GetRootFolderPath() => configModule.AppSettings.SmartUpdateFolder;
        private string GetZipFolderPath() => Path.Combine(configModule.AppSettings.SmartUpdateFolder, "LastDownloads");
        private string GetXmlFilePath() => Path.Combine(configModule.AppSettings.SmartUpdateFolder, "SmartUpdate.xml");
    }
}
