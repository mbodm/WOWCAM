using System.IO;
using WOWCAM.Helper;
using WOWCAM.WebView;

namespace FinalPrototyping.WebView
{
    public sealed class DefaultWebViewConfigurator(IWebViewProvider webViewProvider) : IWebViewConfigurator
    {
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        public void SetDownloadFolder(string downloadFolder)
        {
            var webView = webViewProvider.GetWebView();

            webView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);
        }

        public string GetDownloadFolder()
        {
            var webView = webViewProvider.GetWebView();

            return webView.Profile.DefaultDownloadFolderPath;
        }

        public void EnsureDownloadFolderExists()
        {
            var webView = webViewProvider.GetWebView();

            if (!Directory.Exists(webView.Profile.DefaultDownloadFolderPath))
            {
                Directory.CreateDirectory(webView.Profile.DefaultDownloadFolderPath);
            }
        }

        public Task ClearDownloadFolderAsync(CancellationToken cancellationToken = default)
        {
            var webView = webViewProvider.GetWebView();

            if (!Directory.Exists(webView.Profile.DefaultDownloadFolderPath))
            {
                return Task.CompletedTask;
            }

            return FileSystemHelper.DeleteFolderContentAsync(webView.Profile.DefaultDownloadFolderPath, cancellationToken);
        }
    }
}
