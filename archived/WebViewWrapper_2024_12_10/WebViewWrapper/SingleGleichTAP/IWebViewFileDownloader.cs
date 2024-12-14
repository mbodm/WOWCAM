namespace WebViewWrapper.SingleGleichTAP
{
    public interface IWebViewFileDownloader
    {
        public Task DownloadFileAsync(string downloadUrl, string destFolder, IProgress<bool>? progress = default, CancellationToken cancellationToken = default);
    }
}
