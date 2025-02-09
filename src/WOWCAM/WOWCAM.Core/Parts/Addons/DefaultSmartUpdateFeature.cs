using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.System;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultSmartUpdateFeature(ILogger logger, IAppSettings appSettings, IReliableFileOperations reliableFileOperations) : ISmartUpdateFeature
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));

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
                throw new InvalidOperationException("Error while loading SmartUpdate file: The file is either empty or not a valid XML file.", e);
            }

            var root = doc.Element("wowcam") ?? throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");
            var parent = root.Element("smartupdate") ?? throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            var entries = parent.Elements("entry");
            foreach (var entry in entries)
            {
                var addonName = entry?.Attribute("addonName")?.Value ?? string.Empty;
                var lastDownloadUrl = entry?.Attribute("lastDownloadUrl")?.Value ?? string.Empty;
                var lastZipFile = entry?.Attribute("lastZipFile")?.Value ?? string.Empty;
                var changedAt = entry?.Attribute("changedAt")?.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(addonName) ||
                    string.IsNullOrWhiteSpace(lastDownloadUrl) ||
                    string.IsNullOrWhiteSpace(lastZipFile) ||
                    string.IsNullOrWhiteSpace(changedAt))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains one or more invalid entries.");
                }

                if (!dict.TryAdd(addonName, new SmartUpdateData(addonName, lastDownloadUrl, lastZipFile, changedAt)))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains multiple entries for the same addon.");
                }
            }

            logger.LogMethodExit();
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
                new XAttribute("addonName", kvp.Key),
                new XAttribute("lastDownloadUrl", kvp.Value.DownloadUrl),
                new XAttribute("lastZipFile", kvp.Value.ZipFile),
                new XAttribute("changedAt", kvp.Value.TimeStamp)));

            var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

            await CreateFolderStructureIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

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

            var hasExactEntry = value.AddonName == addonName && value.DownloadUrl == downloadUrl && value.ZipFile == zipFile;
            var zipFileExists = File.Exists(Path.Combine(GetZipFolderPath(), zipFile));

            return hasExactEntry && zipFileExists;
        }

        public async Task AddOrUpdateAddonAsync(string addonName, string downloadUrl, string zipFile, CancellationToken cancellationToken = default)
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

            if (AddonExists(addonName, downloadUrl, zipFile))
            {
                return;
            }

            // Add to dict

            var timeStamp = DateTime.UtcNow.ToIso8601();
            var dictValue = new SmartUpdateData(addonName, downloadUrl, zipFile, timeStamp);
            dict.AddOrUpdate(addonName, dictValue, (_, _) => dictValue);

            // Copy zip file

            await CreateFolderStructureIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            var sourcePath = Path.Combine(GetSourceFolderPath(), zipFile);
            var destPath = Path.Combine(GetZipFolderPath(), zipFile);
            File.Copy(sourcePath, destPath, true);
            // No need for some final IReliableFileOperations delay here (the zip files are independent copy operations in independent tasks and nothing immediately relies on them)
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

        private async Task CreateFolderStructureIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            var rootFolder = GetRootFolderPath();
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            var zipFolder = GetZipFolderPath();
            if (!Directory.Exists(zipFolder))
            {
                Directory.CreateDirectory(zipFolder);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private string GetRootFolderPath() => appSettings.Data.SmartUpdateFolder;
        private string GetZipFolderPath() => Path.Combine(GetRootFolderPath(), "LastDownloads");
        private string GetXmlFilePath() => Path.Combine(GetRootFolderPath(), "SmartUpdate.xml");
        private string GetSourceFolderPath() => appSettings.Data.AddonDownloadFolder;
    }
}
