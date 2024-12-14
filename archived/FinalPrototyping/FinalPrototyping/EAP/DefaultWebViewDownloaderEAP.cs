using System.Collections.Concurrent;
using FinalPrototyping.Logger;
using FinalPrototyping.WebView;
using Microsoft.Web.WebView2.Core;

namespace FinalPrototyping.EAP
{
    public sealed class DefaultWebViewDownloaderEAP(ILogger logger, IWebViewProvider webViewProvider) : IWebViewDownloaderEAP
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly ConcurrentQueue<string> queue = new();
        private int maxDownloads;
        private bool errorOccurred;
        private bool cancellationRequested;

        public event DownloadCompletedEventHandler? DownloadCompleted;
        public event DownloadProgressChangedEventHandler? DownloadProgressChanged;

        public bool IsBusy { get; private set; }

        public void DownloadAsync(IEnumerable<string> downloadUrls, string destFolder)
        {
            ArgumentNullException.ThrowIfNull(downloadUrls);

            if (!downloadUrls.Any())
            {
                throw new ArgumentException("Enumerable is empty.", nameof(downloadUrls));
            }

            if (string.IsNullOrWhiteSpace(destFolder))
            {
                throw new ArgumentException($"'{nameof(destFolder)}' cannot be null or whitespace.", nameof(destFolder));
            }

            if (IsBusy)
            {
                throw new InvalidOperationException(
                    "Busy, cause asynchronous operation is already running (EAP approach of this class does not support multiple concurrent invocations).");
            }

            IsBusy = true;

            maxDownloads = downloadUrls.Count();
            errorOccurred = false;
            cancellationRequested = false;

            var webView = webViewProvider.GetWebView();

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.DownloadStarting += WebView_DownloadStarting;

            webView.Navigate(DequeueNextDownloadUrl());
        }

        public void DownloadAsyncCancel(object userState)
        {
            cancellationRequested = true;
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(WebViewHelper.CreateLogLinesForNavigationStarting(sender, e));

            e.Cancel = cancellationRequested;
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log(WebViewHelper.CreateLogLinesForNavigationCompleted(sender, e));

            if (sender is not CoreWebView2 webView)
            {
                logger.Log("WebView2 raised the 'NavigationCompleted' event, but its 'sender' was invalid.");
                errorOccurred = true;
                return;
            }

            if (!e.IsSuccess)
            {
                logger.Log("WebView2 raised the 'NavigationCompleted' event, but its 'EventArgs.IsSuccess' was 'false'.");
                errorOccurred = true;
                return;
            }

            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
            {
                logger.Log("WebView2 raised the 'NavigationCompleted' event, but its 'EventArgs.WebErrorStatus' was 'OperationCanceled'.");
                errorOccurred = true;
                return;
            }

            webView.Navigate(DequeueNextDownloadUrl());
        }

        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log("Todo: Create log lines for download starting");

            if (errorOccurred || cancellationRequested)
            {
                e.Cancel = true;
                return;
            }

            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            logger.Log("Todo: Create log lines for BytesReceivedChanged");

            if (sender is not CoreWebView2DownloadOperation downloadOperation)
            {
                logger.Log("WebView2 raised the 'BytesReceivedChanged' event, but its 'sender' was invalid.");
                return;
            }

            if (errorOccurred || cancellationRequested)
            {
                downloadOperation.Cancel();
                return;
            }
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            logger.Log("Todo: Create log lines for StateChanged");

            if (sender is not CoreWebView2DownloadOperation downloadOperation)
            {
                logger.Log("WebView2 raised the 'BytesReceivedChanged' event, but its 'sender' was invalid.");
                return;
            }

            if (errorOccurred || cancellationRequested)
            {
                downloadOperation.Cancel();
            }

            if (downloadOperation.State == CoreWebView2DownloadState.Interrupted)
            {
                if (downloadOperation.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled)
                {
                }
            }
            
            if (downloadOperation.State == CoreWebView2DownloadState.Completed)
            {
                OnDownloadProgressChanged();

                if (queue.Count <= 0)
                {
                    OnDownloadCompleted();
                }
            }
        }

        private string DequeueNextDownloadUrl()
        {
            if (queue.TryDequeue(out string? downloadUrl))
            {
                return downloadUrl;
            }

            throw new InvalidOperationException("Could not dequeue next URL from queue.");
        }

        private void OnDownloadCompleted()
        {
            var webView = webViewProvider.GetWebView();

            webView.NavigationStarting -= WebView_NavigationStarting;
            webView.NavigationCompleted -= WebView_NavigationCompleted;
            webView.DownloadStarting -= WebView_DownloadStarting;

            IsBusy = false;

            if (errorOccurred)
            {
                var error = new InvalidOperationException("Todo: An internal error occurred (one of the downloads failed).");
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(error, false));
            }
            else
            {
                var error = new InvalidOperationException("Todo: An internal error occurred (one of the downloads failed).");
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(error, cancellationRequested));
            }
        }

        private void OnDownloadProgressChanged()
        {
            var progressPercentage = CalcPercent();
            DownloadProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(progressPercentage));
        }

        private int CalcPercent()
        {
            // Doing casts inside try/catch block, just to be sure.

            try
            {
                var remainingDownloads = queue.Count;
                var finishedDownloads = maxDownloads - remainingDownloads;

                var exact = (double)(finishedDownloads / maxDownloads) * 100;
                var rounded = (int)Math.Round(exact);
                var percent = rounded > 100 ? 100 : rounded; // Cap it, just to be sure.

                return percent;
            }
            catch
            {
                return 0;
            }
        }
    }
}
