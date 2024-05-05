using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewHelper(ILogger logger) : IWebViewHelper
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private CoreWebView2? coreWebView = null;

        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        public bool IsInitialized => coreWebView != null;

        public bool IsDownloading { get; private set; }

        public Task<CoreWebView2Environment> CreateEnvironmentAsync(string tempFolder)
        {
            // The WebView2 user data folder (UDF) has to have write access and the UDF´s default location is the executable´s folder.
            // Therefore some other folder (with write permissions guaranteed) has to be specified here, used as UDF for the WebView2.
            // Just using the temp folder for the UDF here, since this matches the temporary characteristics the UDF has in this case.
            // Also the application, when started or closed, does NOT try to delete that folder. On purpose! Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there). But
            // in my opinion this is totally fine, since it is a user´s temp folder and the UDF will be reused next time again anyway.

            return CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(tempFolder, "MBODM-WOWCAM-WebView2-UDF"));
        }

        public void Initialize(CoreWebView2 coreWebView, string downloadFolder)
        {
            ArgumentNullException.ThrowIfNull(coreWebView);

            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                throw new ArgumentException($"'{nameof(downloadFolder)}' cannot be null or whitespace.", nameof(downloadFolder));
            }

            if (IsInitialized)
            {
                return;
            }

            coreWebView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

            this.coreWebView = coreWebView;
        }

        public void DownloadAsync(string addonUrl)
        {
            if (string.IsNullOrWhiteSpace(addonUrl))
            {
                throw new ArgumentException($"'{nameof(addonUrl)}' cannot be null or whitespace.", nameof(addonUrl));
            }

            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            if (IsDownloading)
            {
                throw new InvalidOperationException("Download is already running.");
            }

            IsDownloading = true;

            coreWebView.Stop(); // Just to make sure

            coreWebView.NavigationStarting += NavigationStarting;
            coreWebView.NavigationCompleted += NavigationCompleted;
            coreWebView.DownloadStarting += DownloadStarting;

            var addonDownloadUrl = addonUrl.TrimEnd('/') + "/download";

            if (coreWebView.Source.ToString() == addonDownloadUrl)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                coreWebView.Reload();
            }
            else
            {
                coreWebView.Navigate(addonUrl);
            }
        }

        #region WebView2-Events

        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(LogHelper.CreateLines(nameof(NavigationStarting), sender, e,
            [
                $"{nameof(e.IsRedirected)} = {e.IsRedirected}",
                $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.NavigationKind)} = {e.NavigationKind}",
                $"{nameof(e.Uri)} = \"{e.Uri}\"",
            ]));
        }

        private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Notes:
            // 1)
            // At the time of writing this code, the EventArgs e still not including the Uri.
            // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580
            // 2)
            // I also tried using the CoreWebView2.Source property, but this also failed:
            // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/3461
            // 3)
            // Redirects do not raise this event (in contrast to NavigationStarting event).
            // Therefore only the addon page and the final redirect will raise this event.
            // 4)
            // And the CoreWebView2.Source property value also does not change on redirects.

            logger.Log(LogHelper.CreateLines(nameof(NavigationCompleted), sender, e,
            [
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            ]));
        }

        private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log(LogHelper.CreateLines(nameof(DownloadStarting), sender, e,
            [
                $"{nameof(e.ResultFilePath)} = {e.ResultFilePath}"
            ]));

            e.DownloadOperation.BytesReceivedChanged += BytesReceivedChanged;
            e.DownloadOperation.StateChanged += StateChanged;
        }

        private void BytesReceivedChanged(object? sender, object e)
        {
            logger.Log(LogHelper.CreateLines(nameof(BytesReceivedChanged), sender, e, LogHelper.GetDownloadOperationDetails(sender)));

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                if ((ulong)downloadOperation.BytesReceived < downloadOperation.TotalBytesToReceive)
                {
                    // Todo: ?

                    // Only show real chunks and not just the final chunk, when there is only one.
                    // This happens sometimes for mid-sized files. The very small ones create no
                    // event at all. The very big ones create a bunch of events. But for all the
                    // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                    // Therefore it seems OK to ignore them, for better readability of log output.
                }
            }
        }

        private void StateChanged(object? sender, object e)
        {
            logger.Log(LogHelper.CreateLines(nameof(StateChanged), sender, e, LogHelper.GetDownloadOperationDetails(sender)));

            if (sender is CoreWebView2DownloadOperation downloadOperation && downloadOperation.State == CoreWebView2DownloadState.Completed)
            {
                FinishDownload(downloadOperation);
            }
        }

        #endregion

        private void FinishDownload(CoreWebView2DownloadOperation downloadOperation)
        {
            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            coreWebView.NavigationStarting -= NavigationStarting;
            coreWebView.NavigationCompleted -= NavigationCompleted;
            coreWebView.DownloadStarting -= DownloadStarting;

            downloadOperation.BytesReceivedChanged -= BytesReceivedChanged;
            downloadOperation.StateChanged -= StateChanged;

            IsDownloading = false;

            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(null, false, null));
        }
    }
}
