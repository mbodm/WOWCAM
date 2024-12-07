using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public interface IAddonProcessing
    {
        public Task ProcessAddonsAsync(CoreWebView2 coreWebView, IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<ModelAddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
