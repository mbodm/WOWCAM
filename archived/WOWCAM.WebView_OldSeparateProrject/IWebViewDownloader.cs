namespace WOWCAM.WebView
{
    public interface IWebViewDownloader
    {
        public bool HideDownloadDialog { get; set; }

        public Task DownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
