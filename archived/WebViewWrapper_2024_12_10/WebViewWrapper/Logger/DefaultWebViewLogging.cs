using Microsoft.Web.WebView2.Core;

namespace WebViewWrapper.Logger
{
    public sealed class DefaultWebViewLogging : IWebViewLogging
    {
        public IEnumerable<string> CreateLogLinesForNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            return CreateLogLines("NavigationStarting", sender, e,
            [
                $"{nameof(e.IsRedirected)} = {e.IsRedirected}",
                $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.NavigationKind)} = {e.NavigationKind}",
                $"{nameof(e.Uri)} = \"{e.Uri}\""
            ]);
        }

        public IEnumerable<string> CreateLogLinesForNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            return CreateLogLines("NavigationCompleted", sender, e,
            [
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            ]);
        }

        private static IEnumerable<string> CreateLogLines(string name, object? sender, object? e, IEnumerable<string> details)
        {
            var lines = new string[]
            {
                $"{name} (event)",
                $"{nameof(sender)} = {sender}",
                $"{nameof(e)} = {e}"
            };

            return lines.Concat(details);
        }
    }
}
