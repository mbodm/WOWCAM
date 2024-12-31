using System.Globalization;
using System.Xml.Linq;

namespace WOWCAM.Core
{
    public sealed class DefaultSmartUpdateFeature : ISmartUpdateFeature
    {
        private readonly string smartUpdateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM-SmartUpdate.xml");

        public string Storage => smartUpdateFile;
        public bool StorageExists => File.Exists(smartUpdateFile);

        public Task CreateStorageAsync(CancellationToken cancellationToken = default)
        {
            var s = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- ===================================================================== -->
                <!-- Please have a look at https://github.com/mbodm/wowcam for file format -->
                <!-- ===================================================================== -->
                <wowcam>
                	<smartupdate>
                	</smartupdate>
                </wowcam>
                """;

            s += Environment.NewLine;

            return File.WriteAllTextAsync(smartUpdateFile, s, cancellationToken);
        }

        public Task RemoveStorageAsync(CancellationToken cancellationToken = default)
        {
            if (File.Exists(smartUpdateFile))
            {
                File.Delete(smartUpdateFile);
            }

            return Task.Delay(200, cancellationToken);
        }

        public async Task<bool> ExactEntryExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
        {
            var doc = await LoadFileAsync(cancellationToken).ConfigureAwait(false);

            // Not checking file format again (since this was done when file was loaded)

            var entries = doc.Root?.Element("smartupdate")?.Elements("entry");
            var lastDownloadUrl = entries?.Where(entry => entry.Attribute("addonName")?.Name == addonName).FirstOrDefault()?.Attribute("lastDownloadUrl")?.Value ?? string.Empty;

            return lastDownloadUrl.Trim().Equals(downloadUrl.Trim(), StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task AddOrUpdateEntryAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); // ISO 8601 timestamp
            var doc = await LoadFileAsync(cancellationToken).ConfigureAwait(false);

            // Not checking file format again (since this was done when file was loaded)

            doc.Root?.Element("smartupdate")?.Elements("entry")?.Where(entry => entry.Name == )


                // Todo Hier gehts weiter



            doc.Root?.Element("smartupdate")?.Add(new XElement("entry",
                new XAttribute("addonName", addonName), new XAttribute("lastDownloadUrl", downloadUrl), new XAttribute("changedAt", now.ToString())));

            using var fileStream = new FileStream(smartUpdateFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            await doc.SaveAsync(fileStream, SaveOptions.None, cancellationToken).ConfigureAwait(false);
        }

        private async Task<XDocument> LoadFileAsync(CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(smartUpdateFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            if (doc.Root == null || doc.Root.Name != "wowcam")
                throw new InvalidOperationException("Error in SmartUpdate file: The <wowcam> root element not exists.");

            if (doc.Root.Element("smartupdate") == null)
                throw new InvalidOperationException("Error in SmartUpdate file: The <smartupdate> section not exists.");

            return doc;
        }

    }
}
