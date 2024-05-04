using Microsoft.Web.WebView2.Core;

namespace WOWCAM.WebView
{
    public interface IWebViewHelper
    {
        bool IsInitialized { get; }

        Task<CoreWebView2Environment> CreateEnvironmentBeforeInitializeAsync(string configuredTempFolder);
        void Initialize(CoreWebView2 coreWebView, string downloadFolder);
        Task DownloadAddonAsync(string addonUrl, CancellationToken cancellationToken = default);
    }
}
