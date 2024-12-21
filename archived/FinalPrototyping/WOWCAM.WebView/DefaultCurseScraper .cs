using System.Text;
using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;
using WOWCAM.Helper;

namespace WOWCAM.WebView
{
    public sealed class DefaultCurseScraper(ILogger logger, IWebViewProvider webViewProvider) : ICurseScraper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public async Task<string> GetAddonDownloadUrlAsync(string addonPageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonPageUrl))
            {
                throw new ArgumentException($"'{nameof(addonPageUrl)}' cannot be null or whitespace.", nameof(addonPageUrl));
            }

            // Complete process of "navigating to page and executing the JavaScript" has to be one atomic operation. Here is why:
            // Users of this TAP method expect the method to be able to run concurrently (since this is what TAP is designed for).
            // But all WebView2 navigations (incl. their completion) have to run sequentially (cause of how WebView2 is designed).
            // Also WebView2 should not be allowed to navigate again, before JS code execution has finished for the current page.
            // This means: A simple "encapsulate WebView2 EAP into TAP" approach is not enough, to handle the design of WebView2.
            // Therefore a semaphore is used to make sure the whole "navigating to page and executing the JavaScript" runs atomic.

            await semaphore.WaitAsync(cancellationToken);
            await NavigateAsync(addonPageUrl, cancellationToken);
            var addonPageJsonAsBase64 = await ExecuteJavaScriptToFetchJsonAsync();
            semaphore.Release();

            var bytes = Convert.FromBase64String(addonPageJsonAsBase64);
            var json = Encoding.UTF8.GetString(bytes);

            var jsonModel = CurseHelper.SerializeAddonPageJson(json);
            var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);

            return downloadUrl;
        }

        private Task NavigateAsync(string url, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" approach

            var tcs = new TaskCompletionSource();

            // NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.Log(WebViewHelper.CreateLogLinesForNavigationStarting(sender, e));

                e.Cancel = cancellationToken.IsCancellationRequested;
            }

            // NavigationCompleted
            void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                logger.Log(WebViewHelper.CreateLogLinesForNavigationCompleted(sender, e));

                if (sender is CoreWebView2 webView)
                {
                    webView.NavigationStarting -= NavigationStartingEventHandler;
                    webView.NavigationCompleted -= NavigationCompletedEventHandler;

                    if (e.IsSuccess)
                    {
                        tcs.TrySetResult();
                    }
                    else
                    {
                        logger.Log($"WebView2 raised the 'NavigationCompleted' event, but its 'EventArgs.IsSuccess' was false (EventArgs.WebErrorStatus = {e.WebErrorStatus}).");

                        switch (e.WebErrorStatus)
                        {
                            case CoreWebView2WebErrorStatus.OperationCanceled:
                                tcs.TrySetCanceled(cancellationToken);
                                break;
                            case CoreWebView2WebErrorStatus.Timeout:
                                tcs.SetException(new InvalidOperationException("WebView2 connection timeout occurred."));
                                break;
                            default:
                                tcs.SetException(new InvalidOperationException("WebView2 connection error occurred."));
                                break;
                        }
                    }
                }
                else
                {
                    logger.Log("WebView2 raised the 'NavigationCompleted' event, but its 'sender' was invalid.");

                    tcs.TrySetException(new InvalidOperationException("WebView2 event error occurred."));
                }
            }

            var webView = webViewProvider.GetWebView();

            webView.NavigationStarting += NavigationStartingEventHandler;
            webView.NavigationCompleted += NavigationCompletedEventHandler;

            // WebView2 will not raise navigation events for a navigation to an already loaded page (but a reload does)

            if (webView.Source.ToString() == url)
            {
                webView.Reload();
            }
            else
            {
                webView.Navigate(url);
            }

            return tcs.Task;
        }

        private async Task<string> ExecuteJavaScriptToFetchJsonAsync()
        {
            var webView = webViewProvider.GetWebView();

            var scriptResult = await webView.ExecuteScriptWithResultAsync(CurseHelper.FetchJsonScript);
            if (!scriptResult.Succeeded)
            {
                throw new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.Succeeded' was false.");
            }

            var addonPageJsonAsBase64 = scriptResult.ResultAsJson?.TrimStart('"').TrimEnd('"') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(addonPageJsonAsBase64))
            {
                throw new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.ResultAsJson' was null or empty.");
            }

            return addonPageJsonAsBase64;
        }
    }
}
