using Microsoft.Web.WebView2.Core;
using WOWCAM.Logging;

namespace WOWCAM.WebView
{
    public static class LoggerExtensions
    {
        public static void LogWebView2NavigationStarting(this ILogger logger, object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(CreateLogLines("NavigationStarting",
            [
                $"sender          = {sender}",
                $"e               = {e}",
                $"IsRedirected    = {e.IsRedirected}",
                $"IsUserInitiated = {e.IsUserInitiated}",
                $"NavigationId    = {e.NavigationId}",
                $"NavigationKind  = {e.NavigationKind}",
                $"Uri             = \"{e.Uri}\""
            ]));
        }

        public static void LogWebView2NavigationCompleted(this ILogger logger, object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log(CreateLogLines("NavigationCompleted",
            [
                $"sender         = {sender}",
                $"e              = {e}",
                $"HttpStatusCode = {e.HttpStatusCode}",
                $"IsSuccess      = {e.IsSuccess}",
                $"NavigationId   = {e.NavigationId}",
                $"WebErrorStatus = {e.WebErrorStatus}"
            ]));
        }

        public static void LogWebView2DownloadStarting(this ILogger logger, object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log(CreateLogLines("DownloadStarting",
            [
                $"sender              = {sender}",
                $"e                   = {e}",
                $"ResultFilePath      = \"{e.DownloadOperation.ResultFilePath}\"",
                $"TotalBytesToReceive = {e.DownloadOperation.TotalBytesToReceive}",
                $"Uri                 = \"{e.DownloadOperation.Uri}\""
            ]));
        }

        public static void LogWebView2StateChanged(this ILogger logger, object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                logger.Log(CreateLogLines("StateChanged",
                [
                    $"sender              = {sender}",
                    $"e                   = {e}",
                    $"BytesReceived       = {downloadOperation.BytesReceived}",
                    $"InterruptReason     = {downloadOperation.InterruptReason}",
                    $"ResultFilePath      = \"{downloadOperation.ResultFilePath}\"",
                    $"State               = {downloadOperation.State}",
                    $"TotalBytesToReceive = {downloadOperation.TotalBytesToReceive}",
                    $"Uri                 = \"{downloadOperation.Uri}\""
                ]));
            }
            else
            {
                logger.Log(CreateLogLines("StateChanged",
                [
                    $"sender = {sender}",
                    $"e      = {e}",
                ]));
            }
        }

        private static IEnumerable<string> CreateLogLines(string name, IEnumerable<string> details)
        {
            var headerLine = $"{name} (WebView2)";
            var detailsIndented = details.Select(detail => $" => {detail}");

            var result = new List<string> { headerLine };
            result.AddRange(detailsIndented);

            return result.AsEnumerable();
        }
    }
}
