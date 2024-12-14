namespace WebViewTests.WebView
{
    public interface IWebViewConfigurator
    {
        void SetDownloadFolder(string downloadFolder);
        string GetDownloadFolder();
        void EnsureDownloadFolderExists();
        Task ClearDownloadFolderAsync(CancellationToken cancellationToken = default);
    }
}
