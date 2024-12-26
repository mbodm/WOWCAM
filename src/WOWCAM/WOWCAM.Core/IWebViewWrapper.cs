namespace WOWCAM.Core
{
    public record WebViewWrapperDownloadProgress(string DownloadUrl, string FilePath, uint TotalBytes, uint ReceivedBytes);

    public interface IWebViewWrapper
    {
        Task NavigateToPageAsync(string pageUrl, CancellationToken cancellationToken = default);
        Task<string> NavigateToPageAndExecuteJavaScriptAsync(string pageUrl, string javaScript, CancellationToken cancellationToken = default);
        Task NavigateAndDownloadFileAsync(string downloadUrl, IProgress<WebViewWrapperDownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
