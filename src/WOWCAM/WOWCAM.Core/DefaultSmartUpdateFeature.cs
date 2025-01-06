using System.Xml.Linq;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultSmartUpdateFeature : ISmartUpdateFeature
    {
        private readonly string smartUpdateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM-SmartUpdate.xml");

        public string Storage => smartUpdateFile;
        public bool StorageExists => File.Exists(smartUpdateFile);

        public Task CreateStorageIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            if (StorageExists)
            {
                return Task.CompletedTask;
            }

            var s = """
                <?xml version="1.0" encoding="utf-8"?>
                <wowcam>
                	<smartupdate>
                	</smartupdate>
                </wowcam>
                """;

            s += Environment.NewLine;

            return File.WriteAllTextAsync(smartUpdateFile, s, cancellationToken);
        }

        public Task RemoveStorageIfExistsAsync(CancellationToken cancellationToken = default)
        {
            if (StorageExists)
            {
                File.Delete(smartUpdateFile);
            }

            return Task.Delay(200, cancellationToken);
        }

        public async Task<bool> ExactEntryExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            var doc = await LoadFileAsync(smartUpdateFile, cancellationToken).ConfigureAwait(false);

            // Not checking file format again (since this was done when file was loaded)

            var lastDownloadUrl = GetEntryByAddonName(doc, addonName)?.Attribute("lastDownloadUrl")?.Value ?? string.Empty;
            var bothUrlsAreTheSame = lastDownloadUrl.Trim().Equals(downloadUrl.Trim(), StringComparison.CurrentCultureIgnoreCase);

            return bothUrlsAreTheSame;
        }

        public async Task AddOrUpdateEntryAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonName))
            {
                throw new ArgumentException($"'{nameof(addonName)}' cannot be null or whitespace.", nameof(addonName));
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            var now = DateTime.UtcNow.ToIso8601();
            var doc = await LoadFileAsync(smartUpdateFile, cancellationToken).ConfigureAwait(false);

            // Not checking file format again (since this was done when file was loaded)

            var entry = GetEntryByAddonName(doc, addonName);
            if (entry == null)
            {
                doc.Root?.Element("smartupdate")?.Add(new XElement("entry",
                    new XAttribute("addonName", addonName), new XAttribute("lastDownloadUrl", downloadUrl), new XAttribute("changedAt", now)));
            }
            else
            {
                entry.SetAttributeValue("lastDownloadUrl", downloadUrl);
                entry.SetAttributeValue("changedAt", now);
            }

            var sortedEntries = doc.Root?.Element("smartupdate")?.Elements("entry")?.OrderBy(entry => entry.Attribute("addonName")?.Value);
            doc.Root?.Element("smartUpdate")?.ReplaceAll(sortedEntries);

            using var fileStream = new FileStream(smartUpdateFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            await doc.SaveAsync(fileStream, SaveOptions.None, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<XDocument> LoadFileAsync(string xmlFile, CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            if (doc.Root == null || doc.Root.Name != "wowcam")
                throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");

            if (doc.Root.Element("smartupdate") == null)
                throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            return doc;
        }

        private static XElement? GetEntryByAddonName(XDocument document, string addonName)
        {
            var entry = document?.Root?.Element("smartupdate")?.Elements("entry")?.
                Where(entry => (entry.Attribute("addonName")?.Value?.Trim() ?? string.Empty).Equals(addonName?.Trim(), StringComparison.CurrentCultureIgnoreCase)).
                FirstOrDefault();

            return entry;
        }
    }
}
