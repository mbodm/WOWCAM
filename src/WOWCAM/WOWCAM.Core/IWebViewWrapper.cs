namespace WOWCAM.Core
{
    public interface IWebViewWrapper
    {
        Task NavigateToPageAsync(string pageUrl, CancellationToken cancellationToken = default);
        Task NavigateToPageAndExecuteJavaScriptAsync(string pageUrl, string javaScript, CancellationToken cancellationToken = default);
        Task NavigateAndDownloadFileAsync(string downloadUrl, IProgress<WebViewWrapperDownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
