using System.Collections.Concurrent;
using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewDownloaderEAP(ILogger logger, IWebViewProvider webViewProvider) : IWebViewDownloaderEAP
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));


        private readonly ConcurrentQueue<string> queue = new();
        private int maxDownloads;
        private int finishedDownloads;
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
            else
            {
                IsBusy = true;
            }

            finishedDownloads = 0;
            maxDownloads = downloadUrls.Count();

            foreach (var downloadUrl in downloadUrls)
            {
                queue.Enqueue(downloadUrl);
            }

            var webView = webViewProvider.GetWebView();

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.DownloadStarting += WebView_DownloadStarting;

            var url = DequeueNextUrl();
            webView.Navigate(url);
        }

        public void DownloadAsyncCancel()
        {
            cancellationRequested = true;
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log("Todo");
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log("Todo");

            if (sender is CoreWebView2 webView)
            {
                // Do not check success or error here since by default Curse do a 'ConnectionAborted' before the download starts

                if (!queue.IsEmpty)
                {
                    var url = DequeueNextUrl();
                    webView.Navigate(url);
                }
            }
        }

        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            logger.Log("Todo: Create log lines for BytesReceivedChanged");
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            logger.Log("Todo: Create log lines for StateChanged");

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    Interlocked.Increment(ref finishedDownloads);

                    OnDownloadProgressChanged();

                    if (finishedDownloads >= maxDownloads)
                    {
                        OnDownloadCompleted();
                    }
                }
            }
        }

        private string DequeueNextUrl()
        {
            return !queue.IsEmpty && queue.TryDequeue(out string? url) && url != null ? url : string.Empty;
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
                var remainingDownloads = 0;
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
