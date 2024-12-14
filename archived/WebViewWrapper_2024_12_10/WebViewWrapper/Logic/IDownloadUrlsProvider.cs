using Microsoft.Web.WebView2.Core;

namespace WebViewWrapper.Logic
{
    public interface IDownloadUrlsProvider
    {
        Task<IEnumerable<string>> GetAddonDownloadUrls(CoreWebView2 coreWebView, IEnumerable<string> addonPageUrls,
            IProgress<DownloadUrlsProviderProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
