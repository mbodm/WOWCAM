using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewDownloader(ILogger logger, IWebViewProvider webViewProvider) : IWebViewDownloader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly SemaphoreSlim semaphore1 = new(1, 1);

        public async Task DownloadFileAsync(string downloadUrl, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            // The key concept is "navigate sequentially while download concurrently" to get useful TAP handling out of the WebView2 design

            // Process of "navigating to url" has to be one atomic operation, while download can happen asynchronous. Here is why:
            // Users of this TAP method expect the method to be able to run concurrently (since this is what TAP is designed for).
            // But all WebView2 navigations (incl. their completion) have to run sequentially (cause of how WebView2 is designed).
            // After a navigation has completed, WebView2 starts the resulting download asynchronously (does support concurrency).
            // This means: A simple "encapsulate WebView2 EAP into TAP" approach is not enough, to handle the design of WebView2.
            // Therefore a semaphore is used to first run an atomic navigation until download starts (which can run concurrently).

            await semaphore1.WaitAsync(cancellationToken);
            var downloadOperation = await NavigateAndStartDownloadAsync(downloadUrl, cancellationToken);
            semaphore1.Release();

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
                        webView.DownloadStarting += DownloadStartingEventHandler;
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
                logger.Log("Todo: Create log lines for DownloadStarting");

                if (sender is CoreWebView2 webView)
                {
                    webView.DownloadStarting -= DownloadStartingEventHandler;
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
                logger.Log("Todo: Create log lines for BytesReceivedChanged");

                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    progress?.Report(downloadOperation); // Why create a new progress model when CoreWebView2DownloadOperation already has all information we want to report?



                    downloadOperation.b
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
                logger.Log("Todo: Create log lines for StateChanged");

                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    switch (downloadOperation.State)
                    {
                        case CoreWebView2DownloadState.InProgress:
                            // Todo: Since we progress in BytesReceivedChanged there is nothing to do here
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

            downloadOperation.BytesReceivedChanged += BytesReceivedChangedEventHandler;
            downloadOperation.StateChanged += StateChangedEventHandler;

            return tcs.Task;
        }








































        public Task DownloadFileAsync(IEnumerable<string> downloadUrls, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(downloadUrls);


            // This method follows the typical "wrap EAP into TAP" pattern approach

            var tcs = new TaskCompletionSource();

            // Completed
            void DownloadCompletedEventHandler(object sender, DownloadCompletedEventArgs e)
            {
                if (sender is not DefaultWebViewDownloaderEAP webViewDownloader)
                {
                    logger.Log("WebViewDownloaderEAP raised the 'DownloadCompleted' event, but its 'sender' was invalid.");
                    tcs.TrySetException(new InvalidOperationException("Internal event error."));
                    return;
                }

                if (e.Cancelled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else
                {
                    tcs.TrySetResult();
                }
            }

            // ProgressChanged
            void DownloadProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e)
            {
                progress?.Report("Todo: URL");
            }

            var eap = new DefaultWebViewDownloaderEAP(logger, webViewProvider);

            cancellationToken.Register(eap.DownloadAsyncCancel);

            eap.DownloadCompleted += DownloadCompletedEventHandler;
            eap.DownloadProgressChanged += DownloadProgressChangedEventHandler;

            eap.DownloadAsync(downloadUrls, webViewProvider.GetWebView().Profile.DefaultDownloadFolderPath);

            return tcs.Task;
        }
    }
}
