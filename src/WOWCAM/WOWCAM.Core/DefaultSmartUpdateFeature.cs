using System.Collections.Concurrent;
using System.Xml.Linq;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultSmartUpdateFeature : ISmartUpdateFeature
    {
        private readonly string smartUpdateFile = Path.Combine(AppHelper.GetApplicationExecutableFolder(), "WOWCAM.smu");
        private readonly ConcurrentDictionary<string, string> dict = new();

        public string Storage => smartUpdateFile;
        public bool StorageExists => File.Exists(smartUpdateFile);

        public async Task SaveToStorageAsync(CancellationToken cancellationToken = default)
        {
            var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
                new XAttribute("name", kvp.Key),
                new XAttribute("url", kvp.Value)));

            var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

            using var fileStream = new FileStream(smartUpdateFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            await doc.SaveAsync(fileStream, SaveOptions.None, cancellationToken).ConfigureAwait(false);
        }

        public async Task LoadFromStorageIfExistsAsync(CancellationToken cancellationToken = default)
        {
            dict.Clear();

            if (!StorageExists)
            {
                return;
            }

            using var fileStream = new FileStream(smartUpdateFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            var root = doc.Element("wowcam") ??
                throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");

            var parent = root.Element("smartupdate") ??
                throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            var entries = parent.Elements("entry");
            foreach (var entry in entries)
            {
                var addonName = entry?.Attribute("name")?.Value ?? string.Empty;
                var lastDownloadUrl = entry?.Attribute("url")?.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(addonName) || string.IsNullOrWhiteSpace(lastDownloadUrl))
                {
                    throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section contains one or more invalid entries.");
                }

                dict.TryAdd(addonName, lastDownloadUrl);
            }
        }

        public Task RemoveStorageIfExistsAsync(CancellationToken cancellationToken = default)
        {
            if (StorageExists)
            {
                File.Delete(smartUpdateFile);
            }

            return Task.Delay(200, cancellationToken);
        }

        public bool ExactEntryExists(string addonName, string downloadUrl)
        {
            if (!dict.TryGetValue(addonName, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value == downloadUrl;
        }

        public void AddOrUpdateEntry(string addonName, string downloadUrl)
        {
            dict.AddOrUpdate(addonName, downloadUrl, (_, _) => downloadUrl);
        }
    }
}
