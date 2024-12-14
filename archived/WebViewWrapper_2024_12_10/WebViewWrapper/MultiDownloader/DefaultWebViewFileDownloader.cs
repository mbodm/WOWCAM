using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Web.WebView2.Core;
using WebViewWrapper.EAP.Many;
using WebViewWrapper.Helper;
using WebViewWrapper.Logger;
using WebViewWrapper.Provider;

namespace WebViewWrapper.MultiDownloader
{
    public sealed class DefaultWebViewFileDownloader(ILogger logger, IWebViewProvider webViewProvider, IWebViewLogging webViewLogging) : IWebViewFileDownloader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewLogging webViewLogging = webViewLogging ?? throw new ArgumentNullException(nameof(webViewLogging));
        
        private readonly ConcurrentQueue<string> queue = new();
        
        public Task DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder,
            IProgress<string>? progress = default, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" pattern approach

            var tcs = new TaskCompletionSource<string>();

            // NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.Log(webViewLogging.CreateLogLinesForNavigationStarting(sender, e));
            }

            // NavigationCompleted
            async void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                logger.Log(webViewLogging.CreateLogLinesForNavigationCompleted(sender, e));

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
                
                if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                if (queue.TryDequeue(out string? url) && !string.IsNullOrEmpty(url))
                {
                    senderWebView?.Navigate(url);
                }
            }

            foreach (var downloadUrl in downloadUrls)
            {
                queue.Enqueue(downloadUrl);
            }
            
            var webView = webViewProvider.GetWebView();

            cancellationToken.Register(webView.Stop);

            webView.NavigationStarting += NavigationStartingEventHandler;
            webView.NavigationCompleted += NavigationCompletedEventHandler;

            webView.Stop();

            if (queue.TryDequeue(out string? downloadUrlFirst) && !string.IsNullOrWhiteSpace(downloadUrlFirst))
            {
                if (webView.Source.ToString() == downloadUrlFirst)
                {
                    // If the site has already been loaded then the events are not raised without this.
                    // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                    webView.Reload();
                }
                else
                {
                    webView.Navigate(downloadUrlFirst);
                }
            }

            return tcs.Task;
        }
    }
}
