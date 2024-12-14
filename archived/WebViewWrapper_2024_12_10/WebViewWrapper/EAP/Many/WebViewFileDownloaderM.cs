using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace WebViewWrapper.EAP.Many
{
    public sealed class WebViewFileDownloaderM
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private CoreWebView2? coreWebView;
        private ConcurrentQueue<string> queue = new();
        private int maximumDownloads;
        private int finishedDownloads;
        private bool cancellationRequested;

        public bool IsInitialized { get { return coreWebView != null; } }
        public bool IsBusy { get; private set; }

        public event AsyncCompletedEventHandler? DownloadFilesAsyncCompleted;
        public event ProgressChangedEventHandler? DownloadFilesAsyncProgressChanged;

        public Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            // The WebView2 user data folder (UDF) has to have write access and the UDF´s default location is the executable´s folder.
            // Therefore some other folder (with write permissions guaranteed) has to be specified here, used as UDF for the WebView2.
            // Just using the temp folder for the UDF here, since this matches the temporary characteristics the UDF has in this case.
            // Also the application, when started or closed, does NOT try to delete that folder. On purpose! Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there). But
            // in my opinion this is totally fine, since it is a user´s temp folder and the UDF will be reused next time again anyway.

            var userDataFolder = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "MBODM-WOWCAM-WebView2-UDF");

            return CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());
        }

        public void Initialize(CoreWebView2 coreWebView)
        {
            if (coreWebView is null)
            {
                throw new ArgumentNullException(nameof(coreWebView));
            }

            if (this.coreWebView == null)
            {
                this.coreWebView = coreWebView;
            }
        }

        public void DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder)
        {
            if (downloadUrls is null)
            {
                throw new ArgumentNullException(nameof(downloadUrls));
            }

            if (!downloadUrls.Any())
            {
                throw new ArgumentException("Enumerable is empty.", nameof(downloadUrls));
            }

            if (string.IsNullOrWhiteSpace(destFolder))
            {
                throw new ArgumentException($"'{nameof(destFolder)}' cannot be null or whitespace.", nameof(destFolder));
            }

            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            if (IsBusy)
            {
                throw new InvalidOperationException("Download(s) already running.");
            }

            IsBusy = true;
            coreWebView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(destFolder);
            maximumDownloads = downloadUrls.Count();
            finishedDownloads = 0;
            cancellationRequested = false;
            AddHandlers();

            foreach (var downloadUrl in downloadUrls)
            {
                queue.Enqueue(downloadUrl);
            }

            if (queue.TryDequeue(out string? url) && !string.IsNullOrEmpty(url))
            {
                coreWebView.Navigate(url);
            }
        }

        public void CancelDownloadAddonsAsync()
        {
            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            cancellationRequested = true;
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
                            RemoveHandlers();
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
                        RemoveHandlers();
                        OnCompleted(true, "Cancelled by user");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown CoreWebView2DownloadState value.");
                    }
                }
            }
        }

        private void AddHandlers()
        {
            if (coreWebView != null) // Enforced by NRT
            {
                coreWebView.NavigationStarting += NavigationStarting;
                coreWebView.NavigationCompleted += NavigationCompleted;
                coreWebView.DownloadStarting += DownloadStarting;
            }
        }

        private void RemoveHandlers()
        {
            if (coreWebView != null) // Enforced by NRT
            {
                coreWebView.DownloadStarting -= DownloadStarting;
                coreWebView.NavigationCompleted -= NavigationCompleted;
                coreWebView.NavigationStarting -= NavigationStarting;
            }
        }

        private void OnProgressChanged()
        {
            DownloadFilesAsyncProgressChanged?.Invoke(this, new WebViewFileDownloaderProgressChangedEventArgsM(CalcPercent(), null));
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
