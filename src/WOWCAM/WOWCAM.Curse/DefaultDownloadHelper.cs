namespace WOWCAM.Curse
{
    public sealed class DefaultDownloadHelper : IDownloadHelper
    {
        public Task DownloadAddonsAsync(
            IEnumerable<string> downloadUrls, string downloadFolder, IProgress<ModelDownloadHelperProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            var tasks = downloadUrls.Select(downloadUrl => DownloadAddonAsync(
                downloadUrl,
                downloadFolder,
                new Progress<string>(
                    downloadUrl => progress?.Report(
                        new ModelDownloadHelperProgress(downloadUrl, ""))),
                cancellationToken));

            return Task.WhenAll(tasks);
        }

        private static async Task DownloadAddonAsync(string downloadUrl, string downloadFolder, IProgress<string>? progress = default, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient();

            using var response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var fs = File.Create("");

                await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

                progress?.Report(downloadUrl);
            }
        }
    }
}
