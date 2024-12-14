using Microsoft.Web.WebView2.Core;

namespace WebViewWrapper.Logger
{
    public interface IWebViewLogging
    {
        IEnumerable<string> CreateLogLinesForNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e);
        IEnumerable<string> CreateLogLinesForNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e);
    }
}
