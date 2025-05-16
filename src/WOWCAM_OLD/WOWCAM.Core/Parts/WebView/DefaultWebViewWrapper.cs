using System.Text;
using Microsoft.Web.WebView2.Core;
using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.WebView
{
    public sealed class DefaultWebViewWrapper(ILogger logger, IWebViewProvider webViewProvider) : IWebViewWrapper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public bool HideDownloadDialog { get; set; }

        public async Task NavigateToPageAsync(string pageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
            {
                throw new ArgumentException($"'{nameof(pageUrl)}' cannot be null or whitespace.", nameof(pageUrl));
            }

            // Process of "navigating to page" has to be one atomic operation, to prevent concurrent navigation. And here is why:
            // Users of this TAP method expect the method to be able to run concurrently (since this is what TAP is designed for).
            // But all WebView2 navigations (incl. their completion) have to run sequentially (cause of how WebView2 is designed).
            // This means: A simple "encapsulate WebView2 EAP into TAP" approach is not enough, to handle the design of WebView2.
            // Therefore a semaphore is used to make sure "navigating to page" runs atomic, to prevent any concurrent navigation.
            // You may ask "why even bother with TAP then, instead of sync stuff?" and you will find the answer in below methods.

            logger.Log("Navigate to page URL.");
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await NonConcurrentNavigateAsync(pageUrl, false, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<string> NavigateToPageAndExecuteJavaScriptAsync(string pageUrl, string javaScript, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
            {
                throw new ArgumentException($"'{nameof(pageUrl)}' cannot be null or whitespace.", nameof(pageUrl));
            }

            if (string.IsNullOrWhiteSpace(javaScript))
            {
                throw new ArgumentException($"'{nameof(javaScript)}' cannot be null or whitespace.", nameof(javaScript));
            }

            // Complete process of "navigating to page and executing the JavaScript" has to be one atomic operation. Here is why:
            // Users of this TAP method expect the method to be able to run concurrently (since this is what TAP is designed for).
            // But all WebView2 navigations (incl. their completion) have to run sequentially (cause of how WebView2 is designed).
            // Also WebView2 should not be allowed to navigate again, before JS code execution has finished for the current page.
            // This means: A simple "encapsulate WebView2 EAP into TAP" approach is not enough, to handle the design of WebView2.
            // Therefore a semaphore is used to make sure the whole "navigating to page and executing the JavaScript" runs atomic.

            logger.Log("Navigate to page URL and execute JavaScript code.");
            string jsonAsBase64;
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await NonConcurrentNavigateAsync(pageUrl, false, cancellationToken);
                jsonAsBase64 = await ExecuteJavaScriptAsync(javaScript);
            }
            finally
            {
                semaphore.Release();
            }

            var bytes = Convert.FromBase64String(jsonAsBase64);
            var json = Encoding.UTF8.GetString(bytes);

            return json;
        }

        public async Task NavigateAndDownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
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
            // The key concept is "navigate sequentially while download concurrently" to get useful TAP handling out of WebView2.

            logger.Log("Navigate to download URL and start asynchronous download.");
            CoreWebView2DownloadOperation? downloadOperation;
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                downloadOperation = await NonConcurrentNavigateAsync(downloadUrl, true, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }

            if (downloadOperation == null)
            {
                throw new InvalidOperationException("Download navigation failed (WebView2 navigation encapsulation returned null).");
            }

            // I decided against some "navigation progress" report state here (since WebView2 not offers anything to progress).
            // Therefore a "nav-finished & download-starts" progress would be the only progress to give here (which is useless).

            await ConcurrentDownloadAsync(downloadOperation, progress, cancellationToken);
        }

        private Task<CoreWebView2DownloadOperation?> NonConcurrentNavigateAsync(string url, bool isDownload, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" approach (WebView2 navigation part)

            var tcs = new TaskCompletionSource<CoreWebView2DownloadOperation?>();

            // NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.LogWebView2NavigationStarting(sender, e);

                e.Cancel = cancellationToken.IsCancellationRequested;
            }

            // NavigationCompleted
            void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                logger.LogWebView2NavigationCompleted(sender, e);

                if (sender is CoreWebView2 webView)
                {
                    // It's totally fine to remove the handlers here for everyone, regardless of "download" or not.
                    // Cause the "NavigationCompleted" event occurs only once at the end, independent of redirects.

                    webView.NavigationStarting -= NavigationStartingEventHandler;
                    webView.NavigationCompleted -= NavigationCompletedEventHandler;

                    if (e.IsSuccess)
                    {
                        if (isDownload)
                        {
                            // Behaviour is slightly different if URL automatically starts a download after navigation.
                            // For some reason the navigation connection aborts before download starts nonetheless then.
                            // Therefore navigation landing in 'ConnectionAborted' state below (instead of ending here).
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
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
                                if (isDownload)
                                {
                                    // There is some slightly different behaviour between a page URL (ends in "IsSuccess") and a download URL (ends in "DownloadStarting" event).
                                    // For a download URL ignore this error state, cause for some reason the navigation connection aborts before download starts nonetheless then.
                                }
                                else
                                {
                                    goto default;
                                }
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
                logger.LogWebView2DownloadStarting(sender, e);

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

            if (isDownload)
            {
                webView.DownloadStarting += DownloadStartingEventHandler;
            }

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

        private async Task<string> ExecuteJavaScriptAsync(string javaScriptCode)
        {
            var webView = webViewProvider.GetWebView();

            var scriptResult = await webView.ExecuteScriptWithResultAsync(javaScriptCode);
            if (!scriptResult.Succeeded)
            {
                throw new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.Succeeded' was false.");
            }

            var jsonAsBase64 = scriptResult.ResultAsJson?.TrimStart('"').TrimEnd('"') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(jsonAsBase64))
            {
                throw new InvalidOperationException("WebView2 executed the JavaScript code, but the returned 'ScriptResult.ResultAsJson' was null or empty.");
            }

            return jsonAsBase64;
        }

        private Task ConcurrentDownloadAsync(CoreWebView2DownloadOperation downloadOperation,
            IProgress<DownloadProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" approach (WebView2 download part)

            var tcs = new TaskCompletionSource();

            // BytesReceivedChanged
            void BytesReceivedChangedEventHandler(object? sender, object e)
            {
                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    progress?.Report(CreateDownloadProgress(downloadOperation));

                    if (cancellationToken.IsCancellationRequested)
                    {
                        downloadOperation.Cancel();
                    }
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
                logger.Log("WebView2 'StateChanged' event handler reached.");

                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    logger.LogWebView2StateChanged(sender, e);

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
                            // Sadly WebView2 does not raise the 'BytesReceivedChanged' event for very small download files.
                            // Therefore it is important to create a single 100% progress report here, for accurate behavior.
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

            // Normalize the 'CoreWebView2DownloadOperation' long and ulong? discrepancy to an uint (for better/easier progress report handling).
            // An uint fits both (long and ulong) for progress and in my opinion there is just no need to support files larger than 4 GB anyway.

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
