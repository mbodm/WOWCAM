using Microsoft.Web.WebView2.Core;
using WebViewWrapper.FromWOWCAM;
using WebViewWrapper.Helper;

namespace WebViewWrapper.Logic
{
    public sealed class DefaultDownloadUrlsProvider(ICurseScraper curseScraper) : IDownloadUrlsProvider
    {
        private readonly ICurseScraper curseScraper = curseScraper ?? throw new ArgumentNullException(nameof(curseScraper));

        public async Task<IEnumerable<string>> GetAddonDownloadUrls(CoreWebView2 coreWebView, IEnumerable<string> addonPageUrls,
            IProgress<DownloadUrlsProviderProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            // This needs to happen sequential, cause of WebView2 behavior!
            // Therefore do not use concurrency, like Task.WhenAll(), here!

            var result = new List<string>();

            foreach (var addonPageUrl in addonPageUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var addonSlugName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonPageUrl);

                    // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
                    // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.

                    var addonDownloadUrl = await curseScraper.GetAddonDownloadUrlAsync(coreWebView, addonPageUrl);
                    result.Add(addonDownloadUrl);

                    progress?.Report(new DownloadUrlsProviderProgress(addonSlugName, addonPageUrl, addonDownloadUrl));
                }
                catch (Exception e)
                {
                    //logger.Log(e);
                    throw new InvalidOperationException("An error occurred while scraping the download URLs from the Curse addon pages (see log file for details).", e);
                }
            }

            return result;
        }
    }
}
