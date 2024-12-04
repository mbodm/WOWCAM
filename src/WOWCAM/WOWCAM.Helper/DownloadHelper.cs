namespace WOWCAM.Helper
{
    public sealed class DownloadHelper
    {
        public static async Task DownloadFileAsync(HttpClient httpClient, string downloadUrl, string filePath,
            IProgress<DownloadHelperProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpClient);

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }

            if (progress == default)
            {
                using var response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var fileStream = File.Create(filePath);
                await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                fileStream.Close();
            }
            else
            {
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? throw new InvalidOperationException("Could not determine response content length.");
                progress.Report(new DownloadHelperProgress(downloadUrl, true, 0, totalBytes, false));

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var fileStream = File.Create(filePath);

                var buffer = new byte[4096];
                var readBytesNow = 0;
                var readBytesAll = 0;

                while ((readBytesNow = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, readBytesNow), cancellationToken).ConfigureAwait(false);
                    readBytesAll += readBytesNow;

                    var transferFinished = readBytesAll >= totalBytes;
                    progress.Report(new DownloadHelperProgress(downloadUrl, false, readBytesAll, totalBytes, transferFinished));
                }

                if (readBytesAll != totalBytes)
                {
                    throw new InvalidOperationException("Could not read exact same amount of bytes as predicted by content length.");
                }

                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                fileStream.Close();
            }
        }
    }
}
