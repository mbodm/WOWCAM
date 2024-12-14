namespace WebViewWrapper.MultiDownloader
{
    public interface IWebViewFileDownloader
    {
        public Task DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder,
            IProgress<string>? progress = default, CancellationToken cancellationToken = default);
    }
}
