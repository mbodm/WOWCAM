using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Curse
{
    public interface IWebViewHelper
    {
        Task<CoreWebView2Environment> CreateEnvironmentAsync(string tempFolder);

        Task<IEnumerable<string>> GetDownloadUrlsAsync(
            CoreWebView2 coreWebView, IEnumerable<string> addonUrls, IProgress<ModelWebViewHelperProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
