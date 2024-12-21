namespace WOWCAM.WebView
{
    public interface IWebViewDownloader
    {
        public Task DownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
