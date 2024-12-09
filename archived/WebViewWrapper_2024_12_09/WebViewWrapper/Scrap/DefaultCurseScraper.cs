using System.Text;
using Microsoft.Web.WebView2.Core;
using WebViewWrapper.Helper;
using WebViewWrapper.Logger;
using WebViewWrapper.Provider;

namespace WebViewWrapper.Scrap
{
    public sealed class DefaultCurseScraper(ILogger logger, IWebViewProvider webViewProvider) : ICurseScraper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private bool isRunning;

        public async Task<string> GetAddonDownloadUrlAsync(string addonPageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonPageUrl))
            {
                throw new ArgumentException($"'{nameof(addonPageUrl)}' cannot be null or whitespace.", nameof(addonPageUrl));
            }

            if (isRunning)
            {
                throw new InvalidOperationException("Even when this method is a TAP method, it doesn't support real concurrency (cause of the way WebView2 is designed)");
            }

            isRunning = true;

            var webView = webViewProvider.GetWebView();

            var base64 = await FetchJsonAsync(webView, addonPageUrl, cancellationToken);
            var bytes = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(bytes);

            var jsonModel = CurseHelper.SerializeAddonPageJson(json);
            var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);

            isRunning = false;

            return downloadUrl;
        }

        private Task<string> FetchJsonAsync(CoreWebView2 webView, string addonUrl, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" pattern approach

            var tcs = new TaskCompletionSource<string>();

            // NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.Log(CreateLogLinesStarting(sender, e));
            }

            // NavigationCompleted
            async void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                logger.Log(CreateLogLinesCompleted(sender, e));

                if (sender is not CoreWebView2 senderWebView)
                {
                    tcs.TrySetException(new InvalidOperationException("WebView2 raised the 'NavigationCompleted' event, but its 'Sender' was invalid."));
                    return;
                }

                senderWebView.NavigationStarting -= NavigationStartingEventHandler;
                senderWebView.NavigationCompleted -= NavigationCompletedEventHandler;

                if (!e.IsSuccess)
                {
                    tcs.TrySetException(new InvalidOperationException("WebView2 raised the 'NavigationCompleted' event, but its 'EventArgs.IsSuccess' was false."));
                    return;
                }

                var scriptResult = await senderWebView.ExecuteScriptWithResultAsync(CurseHelper.FetchJsonScript);
                if (!scriptResult.Succeeded)
                {
                    tcs.TrySetException(new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.Succeeded' was false."));
                    return;
                }

                var addonPageJsonAsBase64 = scriptResult.ResultAsJson?.TrimStart('"').TrimEnd('"') ?? string.Empty;
                if (string.IsNullOrWhiteSpace(addonPageJsonAsBase64))
                {
                    tcs.TrySetException(new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.ResultAsJson' was null or empty."));
                    return;
                }

                if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                tcs.TrySetResult(addonPageJsonAsBase64);
            }

            cancellationToken.Register(webView.Stop);

            webView.NavigationStarting += NavigationStartingEventHandler;
            webView.NavigationCompleted += NavigationCompletedEventHandler;

            webView.Stop();

            if (webView.Source.ToString() == addonUrl)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                webView.Reload();
            }
            else
            {
                webView.Navigate(addonUrl);
            }

            return tcs.Task;
        }

        private static IEnumerable<string> CreateLogLinesStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
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

        private static IEnumerable<string> CreateLogLinesCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
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
