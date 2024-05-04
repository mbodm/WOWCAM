using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewHelper(ILogger logger) : IWebViewHelper
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private CoreWebView2? coreWebView = null;
        private TaskCompletionSource? taskCompletionSource = null;

        public bool IsInitialized => coreWebView != null;

        public Task<CoreWebView2Environment> CreateEnvironmentBeforeInitializeAsync(string configuredTempFolder)
        {
            // The WebView2 user data folder (UDF) has to have write access and the UDF´s default location is the executable´s folder.
            // Therefore some other folder (with write permissions guaranteed) has to be specified here, used as UDF for the WebView2.
            // Just using the temp folder for the UDF here, since this matches the temporary characteristics the UDF has in this case.
            // Also the application, when started or closed, does NOT try to delete that folder. On purpose! Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there). But
            // in my opinion this is totally fine, since it is a user´s temp folder and the UDF will be reused next time again anyway.

            return CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(configuredTempFolder, "MBODM-WOWCAM-WebView2-UDF"));
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

        public Task DownloadAddonAsync(string addonUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addonUrl))
            {
                throw new ArgumentException($"'{nameof(addonUrl)}' cannot be null or whitespace.", nameof(addonUrl));
            }

            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            taskCompletionSource = new TaskCompletionSource();

            //Todo: Für IProgress dann -> actualAddonName = curseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);

            AddHandlers();

            coreWebView.Stop();

            if (coreWebView.Source.ToString() == addonUrl)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                coreWebView.Reload();
            }
            else
            {
                coreWebView.Navigate(addonUrl);
            }

            // Todo: Don't forget to remove handlers

            return taskCompletionSource.Task;
        }

        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(new string[]
            {
                $"{nameof(NavigationStarting)} event occurred",
                $"{nameof(e.IsRedirected)} = {e.IsRedirected}",
                $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.NavigationKind)} = {e.NavigationKind}",
                $"{nameof(e.Uri)} = {e.Uri}",
            });
        }

        private void DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            logger.Log("WebView2-Event: DOMContentLoaded");
        }

        private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Notes:

            // At the time of writing this code, the EventArgs e still not including the Uri.
            // Therefore relying on NavigationId here, and caching the fetched download URL.
            // This should be no problem, since the navigations are not running concurrently.
            // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

            // I also tried using the CoreWebView2.Source property, instead of caching the
            // fetched download URL in class. But this also failed, cause of another issue.
            // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/3461

            // Note: Redirects do not raise this event (in contrast to the Starting event).
            // Therefore only the addon page and the final redirect will raise this event.
            // Also note: WebView2 does not change its Source property value, on redirects.
            
            logger.Log(new string[]
            {
                $"{nameof(NavigationCompleted)} event occurred",
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            });

        }

        private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log("WebView2:Event:DownloadStarting");

            e.DownloadOperation.StateChanged += StateChanged;
            e.DownloadOperation.BytesReceivedChanged += BytesReceivedChanged;

            //e.Handled = true; // Do not show Microsoft Edge´s default download dialog
        }

        private void BytesReceivedChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation senderDownloadOperation)
            {
                logger.Log("WebView2:Event:BytesReceivedChanged");

                var received = (ulong)senderDownloadOperation.BytesReceived;
                var total = senderDownloadOperation.TotalBytesToReceive ?? 0;

                // Only show real chunks and not just the final chunk, when there is only one.
                // This happens sometimes for mid-sized files. The very small ones create no
                // event at all. The very big ones create a bunch of events. But for all the
                // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                // Therefore it seems OK to ignore them, for better readability of log output.

                if (received < total)
                {
                    logger.Log("SHIBBY --> BytesReceivedChanged: Sind noch nicht alle bytes gewesen.");
                }
            }
        }

        private void StateChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                logger.Log("WebView2:Event:StateChanged");

                if (downloadOperation.State == CoreWebView2DownloadState.InProgress)
                {
                    logger.Log("WebView2:Event:StateChanged: Der 'State' ist 'InProgress' gewesen.");
                }

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    logger.Log("WebView2:Event:StateChanged: Der 'State' ist 'Completed' gewesen.");

                    //taskCompletionSource?.SetResult();

                    logger.Log(e?.ToString() ?? "");
                    downloadOperation.
                }

                if (downloadOperation.State == CoreWebView2DownloadState.Interrupted)
                {
                    logger.Log("SHIBBY --> StateChanged: Der 'State' ist 'Interrupted' gewesen.");
                }
            }
        }
        private void AddHandlers()
        {
            if (coreWebView != null)
            {
                coreWebView.NavigationStarting += NavigationStarting;
                coreWebView.DOMContentLoaded += DOMContentLoaded;
                coreWebView.NavigationCompleted += NavigationCompleted;
                coreWebView.DownloadStarting += DownloadStarting;
            }
        }

        private void RemoveHandlers()
        {
            if (coreWebView != null)
            {
                coreWebView.DownloadStarting -= DownloadStarting;
                coreWebView.NavigationCompleted -= NavigationCompleted;
                coreWebView.DOMContentLoaded -= DOMContentLoaded;
                coreWebView.NavigationStarting -= NavigationStarting;
            }
        }
    }
}
