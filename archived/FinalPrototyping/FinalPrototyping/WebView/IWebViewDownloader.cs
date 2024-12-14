namespace FinalPrototyping.WebView
{
    public interface IWebViewDownloader
    {
        public Task DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder, IProgress<string>? progress = default, CancellationToken cancellationToken = default);
    }
}
