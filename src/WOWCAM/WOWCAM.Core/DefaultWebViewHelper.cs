using System.Text;
using Microsoft.Web.WebView2.Core;
using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultWebViewHelper(ILogger logger, ICurseHelper curseHelper) : IWebViewHelper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

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

        public async Task<ModelAddonDownloadUrlData> GetAddonDownloadUrlDataAsync(CoreWebView2 coreWebView, string addonUrl)
        {
            // No cancellation support here, since there is no load progression for WebView2 and the only thing i could use is e.Cancel in the NavigationStarting event.
            // And to me it makes no sense to support cancellation just to stop before the data transfer even has started. Therefore i decided against cancellation here.

            var base64 = await FetchJsonAsync(coreWebView, addonUrl);

            var bytes = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(bytes);

            var jsonModel = curseHelper.SerializeAddonPageJson(json);
            var downloadUrl = curseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);

            return new ModelAddonDownloadUrlData(downloadUrl, jsonModel.FileName);
        }

        private Task<string> FetchJsonAsync(CoreWebView2 coreWebView, string addonUrl)
        {
            var tcs = new TaskCompletionSource<string>();

            // NavigationStarting
            void NavigationStartingEventHandler(object? sender, CoreWebView2NavigationStartingEventArgs e)
            {
                logger.Log(CreateLogLines(nameof(coreWebView.NavigationStarting), sender, e,
                [
                    $"{nameof(e.IsRedirected)} = {e.IsRedirected}",
                    $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                    $"{nameof(e.NavigationId)} = {e.NavigationId}",
                    $"{nameof(e.NavigationKind)} = {e.NavigationKind}",
                    $"{nameof(e.Uri)} = \"{e.Uri}\"",
                ]));
            }

            // NavigationCompleted
            async void NavigationCompletedEventHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                logger.Log(CreateLogLines(nameof(coreWebView.NavigationCompleted), sender, e,
                [
                    $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                    $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                    $"{nameof(e.NavigationId)} = {e.NavigationId}",
                    $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
                ]));

                if (sender is CoreWebView2 coreWebViewSender)
                {
                    coreWebViewSender.NavigationStarting -= NavigationStartingEventHandler;
                    coreWebViewSender.NavigationCompleted -= NavigationCompletedEventHandler;

                    if (e.IsSuccess)
                    {
                        var scriptResult = await coreWebViewSender.ExecuteScriptWithResultAsync(curseHelper.FetchJsonScript);

                        if (scriptResult.Succeeded)
                        {
                            var addonPageJsonAsBase64 = scriptResult.ResultAsJson.TrimStart('"').TrimEnd('"');

                            tcs.SetResult(addonPageJsonAsBase64);
                        }
                    }
                }
            }

            coreWebView.NavigationStarting += NavigationStartingEventHandler;
            coreWebView.NavigationCompleted += NavigationCompletedEventHandler;

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

            return tcs.Task;
        }

        private static IEnumerable<string> CreateLogLines(string name, object? sender, object? e, IEnumerable<string> details)
        {
            var lines = new string[]
            {
                $"{name} (event)",
                $"{nameof(sender)} = {sender}",
                $"{nameof(e)} = {e}"
            };

            return lines.Concat(details);
        }
    }
}
