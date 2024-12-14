using System.Collections.Concurrent;
using System.Threading;
using FinalPrototyping.Logger;
using Microsoft.Web.WebView2.Core;

namespace FinalPrototyping.WebView
{
    public sealed class DefaultWebViewDownloader(ILogger logger, IWebViewProvider webViewProvider) : IWebViewDownloader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));


        private bool isRunning;
        private TaskCompletionSource? tcs;



        private readonly ConcurrentQueue<string> queue = new();

        public Task DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (isRunning)
            {
                throw new InvalidOperationException("Even when this method is a TAP method, it doesn't support real concurrency (cause of the way WebView2 is designed)");
            }

            isRunning = true;

            // This method, in general, follows the typical "wrap EAP into TAP" pattern approach.
            // In the first second, the TAP stuff here looks weird (like a junior dev coded it).
            // But since the WebView2's design nature is somewhat special, there are just 2 ways
            // you can download all files asynchronously. 1) Either by sticking to the WebView2's
            // EAP pattern approach and forwarding/encapsulating the exsiting EAP design together
            // with some mutex. 2) Or by using the TAP pattern approach without a full concurrency
            // support, but to still have the complete download-process awaitable and make use of
            // IProgress and cancellation via CancellationToken. The key reason for this: You need
            // to start WebView2 navigations sequential, using the blocking Navigate() method. But
            // the downloads themselfes are running asynchronously in the background.
            
            
            
            

            tcs = new TaskCompletionSource();

            foreach (var downloadUrl in downloadUrls)
            {
                if (!string.IsNullOrWhiteSpace(downloadUrl))
                {
                    queue.Enqueue(downloadUrl);
                }
            }

            var webView = webViewProvider.GetWebView();

            cancellationToken.Register(webView.Stop);

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.DownloadStarting += WebView_DownloadStarting;

            webView.Stop();

            var firstDownloadUrl = DequeueNextDownloadUrl();

            if (webView.Source.ToString() == firstDownloadUrl)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                webView.Reload();
            }
            else
            {
                webView.Navigate(firstDownloadUrl);
            }

            return tcs.Task;
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(WebViewHelper.CreateLogLinesForNavigationStarting(sender, e));
        }



        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log("Todo: WebView DownloadStarting event occurred.");

            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            logger.Log("Todo: WebView StateChanged event occurred.");

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                downloadOperation.StateChanged -= DownloadOperation_StateChanged;
            }
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            throw new NotImplementedException();
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log(WebViewHelper.CreateLogLinesForNavigationCompleted(sender, e));

            if (sender is not CoreWebView2 senderWebView)
            {
                tcs?.TrySetException(new InvalidOperationException("WebView2 raised the 'NavigationCompleted' event, but its 'Sender' was invalid."));
                return;
            }

            if (!e.IsSuccess)
            {
                tcs?.TrySetException(new InvalidOperationException("WebView2 raised the 'NavigationCompleted' event, but its 'EventArgs.IsSuccess' was false."));
                return;
            }

            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
            {
                tcs?.TrySetCanceled();
                return;
            }

            var nextUrl = DequeueNextDownloadUrl();
            senderWebView?.Navigate(nextUrl);
        }

        private string DequeueNextDownloadUrl()
        {
            if (queue.TryDequeue(out string? downloadUrl))
            {
                return downloadUrl;
            }

            throw new InvalidOperationException("Could not dequeue next URL from queue.");
        }
    }
}
