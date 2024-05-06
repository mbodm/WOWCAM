﻿using Microsoft.Web.WebView2.Core;
using WOWCAM.Core;

namespace WOWCAM.WebView
{
    public sealed class DefaultWebViewHelper(ILogger logger, ICurseHelper curseHelper) : IWebViewHelper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private CoreWebView2? coreWebView = null;

        public event EventHandler<FetchCompletedEventArgs>? FetchCompleted;

        public bool IsInitialized => coreWebView != null;
        public bool IsFetching { get; private set; }

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

        public void FetchAsync(string addonUrl)
        {
            if (string.IsNullOrWhiteSpace(addonUrl))
            {
                throw new ArgumentException($"'{nameof(addonUrl)}' cannot be null or whitespace.", nameof(addonUrl));
            }

            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            if (IsFetching)
            {
                throw new InvalidOperationException("Fetch is already running.");
            }

            IsFetching = true;

            coreWebView.Stop(); // Just to make sure

            coreWebView.NavigationStarting += NavigationStarting;
            coreWebView.NavigationCompleted += NavigationCompleted;

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
        }

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

        private async void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log(LogHelper.CreateLines(nameof(NavigationCompleted), sender, e,
            [
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            ]));

            if (sender is CoreWebView2 senderWebView && e.IsSuccess)
            {
                var result = await senderWebView.ExecuteScriptWithResultAsync(curseHelper.FetchJsonScript);

                if (result.Succeeded)
                {
                    var addonPageJson = result.ResultAsJson.TrimStart('"').TrimEnd('"');

                    if (coreWebView != null)
                    {
                        coreWebView.NavigationStarting -= NavigationStarting;
                        coreWebView.NavigationCompleted -= NavigationCompleted;
                    }

                    IsFetching = false;

                    FetchCompleted?.Invoke(this, new FetchCompletedEventArgs(addonPageJson, null, false, null));
                }
            }
        }
    }
}
