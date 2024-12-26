using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public sealed class DefaultWebViewProvider : IWebViewProvider
    {
        private CoreWebView2? coreWebView2;

        public void SetWebView(CoreWebView2 coreWebView2)
        {
            ArgumentNullException.ThrowIfNull(coreWebView2);

            this.coreWebView2 = coreWebView2;
        }

        public CoreWebView2 GetWebView()
        {
            if (coreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 not set (please set WebView2 first, by calling the appropriate method).");
            }

            return coreWebView2;
        }
    }
}
