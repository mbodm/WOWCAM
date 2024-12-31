using System.Threading;

namespace WOWCAM.Core
{
    internal sealed class DefaultSmartUpdateFeatureOldWithText : ISmartUpdateFeature
    {
        private readonly string smartUpdateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM-SmartUpdate.txt");

        public string Storage => smartUpdateFile;
        public bool StorageExists => File.Exists(smartUpdateFile);

        public Task CreateStorageAsync(CancellationToken cancellationToken = default)
        {
            return File.WriteAllTextAsync(smartUpdateFile, string.Empty, cancellationToken);
        }

        public Task RemoveStorageAsync(CancellationToken cancellationToken = default)
        {
            if (File.Exists(smartUpdateFile))
            {
                File.Delete(smartUpdateFile);
            }

            return Task.Delay(300, cancellationToken);
        }

        public async Task<bool> ExactEntryExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
        {
            var entry = addonName.Trim().ToLower() + "###" + downloadUrl.Trim().ToLower();
            var lines = await File.ReadAllLinesAsync(smartUpdateFile, cancellationToken).ConfigureAwait(false);
            var exists = lines.Any(line => line.Trim().ToLower() == entry);

            return exists;
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

            addonName = addonName.Trim().ToLower();
            downloadUrl = downloadUrl.Trim().ToLower();

            var searchPattern = addonName + "###";
            var existingLines = await File.ReadAllLinesAsync(smartUpdateFile, cancellationToken).ConfigureAwait(false);
            var existingLinesWithoutSearchedOne = existingLines.Where(line => !line.StartsWith(searchPattern));

            List<string> newLines = [..existingLinesWithoutSearchedOne, $"{addonName}###{downloadUrl}"];
            await File.WriteAllLinesAsync(smartUpdateFile, newLines, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(string addonName, string lastDownloadUrl)> Fuzz(string addonName)
        {
            var lines = await File.ReadAllLinesAsync(smartUpdateFile, cancellationToken).ConfigureAwait(false);
            foreach (var line in lines)
            {
                var lineParts = line.Trim().ToLower().Split("###", StringSplitOptions.RemoveEmptyEntries);
                if (lineParts.Length == 2)
                {
                    var lineAddonName = line.First();
                    var lineLastDownloadUrl = line.Last();
                }
            }
        }
    }
}
