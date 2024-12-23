using Microsoft.Web.WebView2.Core;

namespace WOWCAM.WebView
{
    public sealed class WebViewHelper
    {
        public static Task<CoreWebView2Environment> CreateEnvironmentAsync()
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

        public static IEnumerable<string> CreateLogLinesForNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            return CreateLogLines("NavigationStarting", sender, e,
            [
                $"{nameof(e.IsRedirected)} = {e.IsRedirected}",
                $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.NavigationKind)} = {e.NavigationKind}",
                $"{nameof(e.Uri)} = \"{e.Uri}\""
            ]);
        }

        public static IEnumerable<string> CreateLogLinesForNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            return CreateLogLines("NavigationCompleted", sender, e,
            [
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)} = {e.IsSuccess}",
                $"{nameof(e.NavigationId)} = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            ]);
        }

        public static IEnumerable<string> CreateLogLinesForDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            return CreateLogLines("DownloadStarting", sender, e,
            [
                $"{nameof(e.DownloadOperation.ResultFilePath)} = \"{e.DownloadOperation.ResultFilePath}\"",
                $"{nameof(e.DownloadOperation.TotalBytesToReceive)} = {e.DownloadOperation.TotalBytesToReceive}",
                $"{nameof(e.DownloadOperation.Uri)} = \"{e.DownloadOperation.Uri}\""
            ]);
        }

        public static IEnumerable<string> CreateLogLinesForStateChanged(object? sender, object e)
        {
            IEnumerable<string> details = [];

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                details = [
                    $"{nameof(downloadOperation.BytesReceived)} = {downloadOperation.BytesReceived}",
                    $"{nameof(downloadOperation.InterruptReason)} = {downloadOperation.InterruptReason}",
                    $"{nameof(downloadOperation.ResultFilePath)} = \"{downloadOperation.ResultFilePath}\"",
                    $"{nameof(downloadOperation.State)} = {downloadOperation.State}",
                    $"{nameof(downloadOperation.TotalBytesToReceive)} = {downloadOperation.TotalBytesToReceive}",
                    $"{nameof(downloadOperation.Uri)} = \"{downloadOperation.Uri}\""
                ];
            }

            return CreateLogLines("StateChanged", sender, e, details);
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
