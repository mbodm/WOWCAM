using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using WOWCAM.Core.Parts.Extensions;
using WOWCAM.Core.Parts.Logic.System;
using WOWCAM.Logging;

namespace WOWCAM.Core.Parts.Logic.Addons
{
    public sealed class DefaultSmartUpdateFeature(ILogger logger, IReliableFileOperations reliableFileOperations) : ISmartUpdateFeature
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));

        private readonly ConcurrentDictionary<string, SmartUpdateData> dict = new();

        private string rootFolder = string.Empty;
        private string zipFolder = string.Empty;
        private string xmlFile = string.Empty;
        private string addonsDownloadFolder = string.Empty;
        private bool isInitialized = false;

        public async Task InitAsync(string smartUpdateFolder, string addonsDownloadFolder, CancellationToken cancellationToken = default)
        {
            if (isInitialized)
            {
                return;
            }

            rootFolder = smartUpdateFolder;
            zipFolder = Path.Combine(rootFolder, "PreviousDownloads");
            xmlFile = Path.Combine(rootFolder, "SmartUpdate.xml");
            this.addonsDownloadFolder = addonsDownloadFolder;

            // It is better to check (and maybe create) the folders once at startup than checking them for every single addon knocking at the door

            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!Directory.Exists(zipFolder))
            {
                Directory.CreateDirectory(zipFolder);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            isInitialized = true;
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            CheckInitialization();

            dict.Clear();

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
                var previousDownloadUrl = entry?.Attribute("previousDownloadUrl")?.Value ?? string.Empty;
                var previousZipFile = entry?.Attribute("previousZipFile")?.Value ?? string.Empty;
                var changedAt = entry?.Attribute("changedAt")?.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(addonName) ||
                    string.IsNullOrWhiteSpace(previousDownloadUrl) ||
                    string.IsNullOrWhiteSpace(previousZipFile) ||
                    string.IsNullOrWhiteSpace(changedAt))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains one or more invalid entries.");
                }

                var zipFilePath = Path.Combine(zipFolder, previousZipFile);
                if (!File.Exists(zipFilePath))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The XML file and the corresponding zip folder are not in sync.");
                }

                if (!dict.TryAdd(addonName, new SmartUpdateData(addonName, previousDownloadUrl, previousZipFile, changedAt)))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains multiple entries for the same addon.");
                }
            }

            logger.LogMethodExit();
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            CheckInitialization();

            var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
                new XAttribute("addonName", kvp.Key),
                new XAttribute("previousDownloadUrl", kvp.Value.DownloadUrl),
                new XAttribute("previousZipFile", kvp.Value.ZipFile),
                new XAttribute("changedAt", kvp.Value.TimeStamp)));

            var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

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

            CheckInitialization();

            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
            {
                return false;
            }

            var hasExactEntry = value.AddonName == addonName && value.DownloadUrl == downloadUrl && value.ZipFile == zipFile;
            var zipFileExists = File.Exists(Path.Combine(zipFolder, zipFile));

            return hasExactEntry && zipFileExists;
        }

        public void AddOrUpdateAddon(string addonName, string downloadUrl, string zipFile)
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

            CheckInitialization();

            if (AddonExists(addonName, downloadUrl, zipFile))
            {
                return;
            }

            // Remove old zip file (if it's an update)

            if (dict.ContainsKey(addonName))
            {
                if (dict.TryGetValue(addonName, out SmartUpdateData? value) && value != null && !string.IsNullOrWhiteSpace(value.ZipFile))
                {
                    var oldZipFile = Path.Combine(zipFolder, value.ZipFile);
                    if (File.Exists(oldZipFile))
                    {
                        File.Delete(oldZipFile);
                        // No need for some final IReliableFileOperations delay here (the zip files are independent copy operations in independent tasks and nothing immediately relies on them)
                    }
                }
            }

            // Add new to dict

            var timeStamp = DateTime.UtcNow.ToIso8601();
            var dictValue = new SmartUpdateData(addonName, downloadUrl, zipFile, timeStamp);
            dict.AddOrUpdate(addonName, dictValue, (_, _) => dictValue);

            // Save new zip file

            var sourcePath = Path.Combine(addonsDownloadFolder, zipFile);
            var destPath = Path.Combine(zipFolder, zipFile);
            File.Copy(sourcePath, destPath, true);
            // No need for some final IReliableFileOperations delay here (the zip files are independent copy operations in independent tasks and nothing immediately relies on them)
        }

        public void DeployZipFile(string addonName)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            CheckInitialization();

            if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
            {
                throw new InvalidOperationException("SmartUpdate could not found an existing entry for given addon name.");
            }

            var zipFile = value.ZipFile;
            if (string.IsNullOrWhiteSpace(zipFile))
            {
                throw new InvalidOperationException("SmartUpdate could not determine the zip name for given addon name.");
            }

            var zipFilePath = Path.Combine(zipFolder, zipFile);
            if (!File.Exists(zipFilePath))
            {
                throw new InvalidOperationException("SmartUpdate could not found an existing zip file for given addon name.");
            }

            var sourcePath = zipFilePath;
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(addonsDownloadFolder, fileName);
            File.Copy(sourcePath, destPath, true);
            // No need for some final IReliableFileOperations delay here (the zip files are independent copy operations in independent tasks and nothing immediately relies on them)
        }

        private void CheckInitialization()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("SmartUpdate feature is not initialized (please initialize the feature first, by calling the appropriate method.");
            }
        }
    }
}
