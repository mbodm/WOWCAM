namespace WOWCAM.WebView
{
    public interface IWebViewWrapper
    {
        public bool HideDownloadDialog { get; set; }

        Task NavigateToPageAsync(string pageUrl, CancellationToken cancellationToken = default);
        Task<string> NavigateToPageAndExecuteJavaScriptAsync(string pageUrl, string javaScript, CancellationToken cancellationToken = default);
        Task NavigateAndDownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
