using Microsoft.Web.WebView2.Core;

namespace FinalPrototyping.WebView
{
    public interface IWebViewProvider
    {
        void SetWebView(CoreWebView2 coreWebView2);
        CoreWebView2 GetWebView();
    }
}
