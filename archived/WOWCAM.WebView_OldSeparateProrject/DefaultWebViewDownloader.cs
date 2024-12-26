using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewDownloader(ILogger logger, IWebViewProvider webViewProvider) : IWebViewDownloader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public bool HideDownloadDialog { get; set; }

        // The key concept is "navigate sequentially while download concurrently" to get useful TAP handling out of the WebView2 design

        public async Task DownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            // Process of "navigating to url" has to be one atomic operation, while download can happen asynchronous. Here is why:
            // Users of this TAP method expect the method to be able to run concurrently (since this is what TAP is designed for).
            // But all WebView2 navigations (incl. their completion) have to run sequentially (cause of how WebView2 is designed).
            // After a navigation has completed, WebView2 starts the resulting download asynchronously (does support concurrency).
            // This means: A simple "encapsulate WebView2 EAP into TAP" approach is not enough, to handle the design of WebView2.
            // Therefore a semaphore is used to first run an atomic navigation, till download starts (which can run concurrently).

            await semaphore.WaitAsync(cancellationToken);
            var downloadOperation = await NavigateAndStartDownloadAsync(downloadUrl, cancellationToken);
            semaphore.Release();

            // I decided against some "navigation progress" report state here (since WebView2 not offers anything to progress)
            // Therefore a "nav-finished & download-starts" progress would be the only progress to give here (which is useless)

            await DownloadAsync(downloadOperation, progress, cancellationToken);
        }

        private Task<CoreWebView2DownloadOperation> NavigateAndStartDownloadAsync(string url, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" approach (WebView2 navigation part)

            var tcs = new TaskCompletionSource<CoreWebView2DownloadOperation>();

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
                        // For some reason the navigation connections aborts before download starts nonetheless then
                        // Therefore navigation never ends here (instead of always ends in 'ConnectionAborted' below)
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
                                tcs.SetException(new InvalidOperationException("WebView2 connection timeout occurred, while navigating."));
                                break;
                            case CoreWebView2WebErrorStatus.ConnectionAborted:
                                // We have to ignore this error state since for some reason the navigation connections aborts before download starts nonetheless then
                                break;
                            default:
                                tcs.SetException(new InvalidOperationException("WebView2 connection error occurred, while navigating."));
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

            // DownloadStarting
            void DownloadStartingEventHandler(object? sender, CoreWebView2DownloadStartingEventArgs e)
            {
                logger.Log(WebViewHelper.CreateLogLinesForDownloadStarting(sender, e));

                if (sender is CoreWebView2 webView)
                {
                    webView.DownloadStarting -= DownloadStartingEventHandler;
                    e.Handled = HideDownloadDialog; // An "e.Handled = true" means -> Do not show Microsoft Edge´s default download dialog
                    e.Cancel = cancellationToken.IsCancellationRequested;
                    tcs.TrySetResult(e.DownloadOperation);
                }
                else
                {
                    logger.Log("WebView2 raised the 'DownloadStarting' event, but its 'sender' was invalid.");
                    tcs.TrySetException(new InvalidOperationException("WebView2 event error occurred."));
                }
            }

            var webView = webViewProvider.GetWebView();

            webView.NavigationStarting += NavigationStartingEventHandler;
            webView.NavigationCompleted += NavigationCompletedEventHandler;
            webView.DownloadStarting += DownloadStartingEventHandler;

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

        private Task DownloadAsync(CoreWebView2DownloadOperation downloadOperation, IProgress<DownloadProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" approach (WebView2 download part)

            var tcs = new TaskCompletionSource();

            // BytesReceivedChanged
            void BytesReceivedChangedEventHandler(object? sender, object e)
            {
                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    progress?.Report(CreateDownloadProgress(downloadOperation));
                }
                else
                {
                    logger.Log("WebView2 raised the download 'BytesReceivedChanged' event, but its 'sender' was invalid.");
                    tcs.TrySetException(new InvalidOperationException("WebView2 event error occurred."));
                }
            }

            // StateChanged
            void StateChangedEventHandler(object? sender, object e)
            {
                logger.Log("WebView2 StateChanged event handler reached.");

                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    logger.Log(WebViewHelper.CreateLogLinesForStateChanged(sender, e));

                    switch (downloadOperation.State)
                    {
                        case CoreWebView2DownloadState.InProgress:
                            // Nothing to do here
                            break;
                        case CoreWebView2DownloadState.Interrupted:
                            switch (downloadOperation.InterruptReason)
                            {
                                case CoreWebView2DownloadInterruptReason.UserCanceled:
                                    tcs.TrySetCanceled(cancellationToken);
                                    break;
                                case CoreWebView2DownloadInterruptReason.NetworkTimeout:
                                    tcs.SetException(new InvalidOperationException("WebView2 connection timeout occurred, while downloading."));
                                    break;
                                default:
                                    tcs.SetException(new InvalidOperationException("WebView2 connection error occurred, while downloading."));
                                    break;
                            }
                            break;
                        case CoreWebView2DownloadState.Completed:
                            // Sadly WebView2 does not raise the 'BytesReceivedChanged' event for very small download files
                            // Therefore it is important to create a single 100% progress report here for accurate behavior
                            progress?.Report(CreateDownloadProgress(downloadOperation));
                            tcs.TrySetResult();
                            break;
                        default:
                            tcs.TrySetException(new InvalidOperationException("WebView2 used an unknown 'CoreWebView2DownloadOperation.State' value."));
                            break;
                    }
                }
                else
                {
                    logger.Log("WebView2 raised the download 'StateChanged' event, but its 'sender' was invalid.");
                    tcs.TrySetException(new InvalidOperationException("WebView2 event error occurred."));
                }
            }

            // Normalize the 'CoreWebView2DownloadOperation' long and ulong? discrepancy to an uint (for better/easier progress report handling)
            // An uint fits both (long and ulong) for progress and in my opinion there is just no need to support files larger than 4 GB anyway

            if (downloadOperation.TotalBytesToReceive < uint.MaxValue)
            {
                downloadOperation.BytesReceivedChanged += BytesReceivedChangedEventHandler;
                downloadOperation.StateChanged += StateChangedEventHandler;
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException("This implementation does not support the download of files larger than 4 GB."));
            }

            return tcs.Task;
        }

        private static DownloadProgress CreateDownloadProgress(CoreWebView2DownloadOperation downloadOperation)
        {
            var totalBytes = 0u;
            var receivedBytes = 0u;

            // Make sure WebView2 provides the total size and make sure to get no cast error (even when max total size was already checked above before download started)

            if (downloadOperation.TotalBytesToReceive != null && downloadOperation.TotalBytesToReceive < uint.MaxValue)
            {
                totalBytes = (uint)downloadOperation.TotalBytesToReceive;
                receivedBytes = (uint)downloadOperation.BytesReceived;
            }

            // Otherwise use 0 for total bytes and 0 for received bytes

            return new DownloadProgress(downloadOperation.Uri, downloadOperation.ResultFilePath, totalBytes, receivedBytes);
        }
    }
}
