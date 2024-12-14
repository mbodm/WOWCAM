using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Core;
using WebViewWrapper.EAP.Single;
using WebViewWrapper.Logger;
using WebViewWrapper.Provider;

namespace WebViewWrapper.EAP
{
    public sealed class WebViewFileDownloaderEAP(ILogger logger, IWebViewProvider webViewProvider) : IWebViewFileDownloaderEAP
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));


        private CoreWebView2? webView;



        private ConcurrentQueue<string> queue = new();
        private int maximumDownloads;
        private int finishedDownloads;
        private bool cancellationRequested;

        public event DownloadFileCompletedEventHandler? DownloadFileCompleted;
        public event DownloadFileProgressChangedEventHandler? DownloadFileProgressChanged;

        public void DownloadFileAsync(string downloadUrl, string destFolder, object? userState)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException($"'{nameof(downloadUrl)}' cannot be null or whitespace.", nameof(downloadUrl));
            }

            if (string.IsNullOrWhiteSpace(destFolder))
            {
                throw new ArgumentException($"'{nameof(destFolder)}' cannot be null or whitespace.", nameof(destFolder));
            }

            var webView = webViewProvider.GetWebView();
            if (webView == null)
            {
                throw new InvalidOperationException("Todo");
            }

            this.webView = webView;
            this.webView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(destFolder);
            cancellationRequested = false;
            AddWebViewHandler();

            webView.Navigate(downloadUrl);
        }

        public void DownloadFileAsyncCancel(object userState)
        {
            cancellationRequested = true;
        }

        private void AddWebViewHandler()
        {
            if (webView != null)
            {
                webView.NavigationStarting += NavigationStarting;
                webView.NavigationCompleted += NavigationCompleted;
                webView.DownloadStarting += DownloadStarting;
            }
        }

        private void RemoveWebViewHandlerHandler()
        {
            if (webView != null)
            {
                webView.DownloadStarting -= DownloadStarting;
                webView.NavigationCompleted -= NavigationCompleted;
                webView.NavigationStarting -= NavigationStarting;
            }
        }

        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (sender is CoreWebView2 senderCoreWebView)
            {
                Debug.WriteLine("==================================================");
                Debug.WriteLine("NavigationStarting");
                Debug.WriteLine(GetTimeStamp());
                //Debug.WriteLine($"{nameof(e.IsRedirected)} = {e.IsRedirected}");
                //Debug.WriteLine($"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}");
                Debug.WriteLine($"{nameof(e.NavigationId)} = {e.NavigationId}");
                //Debug.WriteLine($"{nameof(e.NavigationKind)} = {e.NavigationKind}");
                Debug.WriteLine($"{nameof(e.Uri)} = {e.Uri}");
                Debug.WriteLine("==================================================");
            }
        }

        private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (sender is CoreWebView2 senderCoreWebView)
            {
                Debug.WriteLine("==================================================");
                Debug.WriteLine("NavigationCompleted");
                Debug.WriteLine(GetTimeStamp());
                Debug.WriteLine($"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}");
                //Debug.WriteLine($"{nameof(e.IsSuccess)} = {e.IsSuccess}");
                Debug.WriteLine($"{nameof(e.NavigationId)} = {e.NavigationId}");
                //Debug.WriteLine($"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}");
                Debug.WriteLine("==================================================");
            }

            if (queue.TryDequeue(out string? url) && !string.IsNullOrEmpty(url))
            {
                coreWebView?.Navigate(url);
            }
        }

        private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            /*
            CoreWebView2Deferral deferral = e.GetDeferral();
            SynchronizationContext.Current?.Post((_) =>
            {
                using (deferral)
                {
                    e.Handled = true;
                    e.ResultFilePath = e.ResultFilePath;
                }
            }, null);
            */

            e.DownloadOperation.StateChanged += StateChanged;
            //e.DownloadOperation.BytesReceivedChanged += BytesReceivedChanged;

            Debug.WriteLine("==================================================");
            Debug.WriteLine("DownloadStarting");
            Debug.WriteLine(GetTimeStamp());
            Debug.WriteLine($"{nameof(e.DownloadOperation.Uri)} = {e.DownloadOperation.Uri}");
            //Debug.WriteLine($"{nameof(e.ResultFilePath)} = {e.ResultFilePath}");
            //Debug.WriteLine($"{nameof(e.DownloadOperation.TotalBytesToReceive)} = {e.DownloadOperation.TotalBytesToReceive}");
            Debug.WriteLine("==================================================");

            //e.Handled = true; // Do not show Microsoft Edge´s default download dialog
        }

        private void BytesReceivedChanged(object? sender, object e)
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
                    Debug.WriteLine("==================================================");
                    Debug.WriteLine("BytesReceivedChanged");
                    Debug.WriteLine(GetTimeStamp());
                    Debug.WriteLine($"{nameof(senderDownloadOperation.Uri)} = {senderDownloadOperation.Uri}");
                    //Debug.WriteLine($"{nameof(senderDownloadOperation.ResultFilePath)} = {senderDownloadOperation.ResultFilePath}");
                    Debug.WriteLine($"{nameof(senderDownloadOperation.BytesReceived)} = {senderDownloadOperation.BytesReceived}");
                    Debug.WriteLine("==================================================");

                    // Doing this inside above if clause, allows small file downloads to finish.

                    if (cancellationRequested)
                    {
                        senderDownloadOperation.Cancel();
                    }
                }
            }
        }

        private void StateChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation senderDownloadOperation)
            {
                Debug.WriteLine("==================================================");
                Debug.WriteLine("StateChanged");
                Debug.WriteLine(GetTimeStamp());
                Debug.WriteLine($"{nameof(senderDownloadOperation.Uri)} = {senderDownloadOperation.Uri}");
                Debug.WriteLine("==================================================");

                if (senderDownloadOperation.State == CoreWebView2DownloadState.InProgress)
                {
                    // Todo: Log() -> "Warning: CoreWebView2DownloadState is 'InProgress' and usually this not happens! Anyway, download will continue."
                }
                else
                {
                    senderDownloadOperation.StateChanged -= StateChanged;
                    senderDownloadOperation.BytesReceivedChanged -= BytesReceivedChanged;

                    if (senderDownloadOperation.State == CoreWebView2DownloadState.Completed)
                    {
                        finishedDownloads++;

                        if (finishedDownloads >= maximumDownloads)
                        {
                            RemoveWebViewHandlerHandler();
                            OnProgressChanged();
                            OnCompleted();
                        }
                        else
                        {
                            OnProgressChanged();
                        }
                    }
                    else if (senderDownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                    {
                        RemoveWebViewHandlerHandler();
                        OnCompleted(true, "Cancelled by user");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown CoreWebView2DownloadState value.");
                    }
                }
            }
        }

        private void OnProgressChanged()
        {
            DownloadFilesAsyncProgressChanged?.Invoke(this, new WebViewFileDownloaderProgressChangedEventArgs(CalcPercent(), null));
        }

        private void OnCompleted(bool cancelled = false, string error = "")
        {
            IsBusy = false;

            DownloadFilesAsyncCompleted?.Invoke(this, new AsyncCompletedEventArgs(error != string.Empty ? new InvalidOperationException(error) : null, cancelled, null));
        }

        private int CalcPercent()
        {
            // Doing casts inside try/catch block, just to be sure.

            try
            {
                var exact = (double)100 / queue.Count() * finishedDownloads;
                var rounded = (int)Math.Round(exact);
                var percent = rounded > 100 ? 100 : rounded; // Cap it, just to be sure.

                return percent;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetTimeStamp()
        {
            return DateTime.Now.ToString("HH:mm:ss:fff");
        }
    }
}
