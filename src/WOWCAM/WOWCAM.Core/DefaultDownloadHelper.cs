namespace WOWCAM.Core
{
    public sealed class DefaultDownloadHelper(HttpClient httpClient) : IDownloadHelper
    {
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task DownloadAddonAsync(string downloadUrl, string filePath, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var fs = File.Create(filePath);

                await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

                fs.Close();
            }
        }
    }
}
