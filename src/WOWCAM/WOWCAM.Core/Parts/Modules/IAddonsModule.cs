using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core.Parts.Modules
{
    public interface IAddonsModule
    {
        public bool HideDownloadDialog { get; set; }

        void SetWebView(CoreWebView2 webView);
        Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = null, CancellationToken cancellationToken = default);
    }
}
