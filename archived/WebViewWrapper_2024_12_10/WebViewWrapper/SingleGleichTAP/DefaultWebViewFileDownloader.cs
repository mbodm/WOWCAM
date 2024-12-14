using System.Collections.Concurrent;
using Microsoft.Web.WebView2.Core;
using WebViewWrapper.Logger;
using WebViewWrapper.Provider;

namespace WebViewWrapper.SingleGleichTAP
{
    public sealed class DefaultWebViewFileDownloader(ILogger logger, IWebViewProvider webViewProvider, IWebViewLogging webViewLogging) : IWebViewFileDownloader
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewLogging webViewLogging = webViewLogging ?? throw new ArgumentNullException(nameof(webViewLogging));

        public Task DownloadFileAsync(string downloadUrl, string destFolder, IProgress<bool>? progress = default, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" pattern approach

            var tcs = new TaskCompletionSource<string>();

            // WebView->NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.Log(webViewLogging.CreateLogLinesForNavigationStarting(sender, e));
            }

            // WebView->NavigationCompleted
            void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
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

                senderWebView.DownloadStarting += DownloadStartingEventHandler;

                if (queue.TryDequeue(out string? url) && !string.IsNullOrEmpty(url))
                {
                    senderWebView?.Navigate(url);
                }
            }

            // WebView->DownloadStarting
            void DownloadStartingEventHandler(object? sender, CoreWebView2DownloadStartingEventArgs e)
            {
                e.DownloadOperation.StateChanged += StateChangedEventHandler;
                e.DownloadOperation.BytesReceivedChanged += BytesReceivedChangedEventHandler;

                //e.Handled = true; // Do not show Microsoft Edge´s default download dialog
            }

            // WebView->BytesReceivedChanged
            void BytesReceivedChangedEventHandler(object? sender, object e)
            {
                if (sender is CoreWebView2DownloadOperation senderDownloadOperation)
                {
                    var receivedBytes = (ulong)senderDownloadOperation.BytesReceived;
                    var totalBytes = senderDownloadOperation.TotalBytesToReceive ?? 0;

                    // Only show real chunks and not just the final chunk, when there is only one.
                    // This happens sometimes for mid-sized files. The very small ones create no
                    // event at all. The very big ones create a bunch of events. But for all the
                    // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                    // Therefore it seems OK to ignore them, for better readability of log output.

                    if (receivedBytes < totalBytes)
                    {
                        // Doing this inside above if clause, allows small file downloads to finish.

                        if (cancellationToken.IsCancellationRequested)
                        {
                            senderDownloadOperation.Cancel();
                        }
                    }
                }
            }

            // WebView->StateChanged
            void StateChangedEventHandler(object? sender, object e)
            {
                if (sender is CoreWebView2DownloadOperation senderDownloadOperation)
                {
                    if (senderDownloadOperation.State == CoreWebView2DownloadState.InProgress)
                    {
                        // Todo: Log() -> "Warning: CoreWebView2DownloadState is 'InProgress' and usually this not happens! Anyway, download will continue."
                    }
                    else
                    {
                        senderDownloadOperation.StateChanged -= StateChangedEventHandler;
                        senderDownloadOperation.BytesReceivedChanged -= BytesReceivedChangedEventHandler;

                        if (senderDownloadOperation.State == CoreWebView2DownloadState.Completed)
                        {
                            tcs.TrySetResult(senderDownloadOperation.Uri);
                        }
                        else if (senderDownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                        {
                            if (senderDownloadOperation.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled)
                            {
                                tcs.TrySetCanceled(cancellationToken);
                            }
                            else
                            {
                                tcs.TrySetException(new InvalidOperationException($"Download was interrupted with '{senderDownloadOperation.InterruptReason}' reason."));
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown {nameof(CoreWebView2DownloadState)} value.");
                        }
                    }
                }
            }

            var webView = webViewProvider.GetWebView();

            cancellationToken.Register(webView.Stop);

            webView.NavigationStarting += NavigationStartingEventHandler;
            webView.NavigationCompleted += NavigationCompletedEventHandler;

            webView.Stop();

            if (webView.Source.ToString() == downloadUrl)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                webView.Reload();
            }
            else
            {
                webView.Navigate(downloadUrl);
            }

            return tcs.Task;
        }
    }
}
