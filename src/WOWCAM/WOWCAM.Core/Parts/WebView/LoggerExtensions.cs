using Microsoft.Web.WebView2.Core;
using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.WebView
{
    public static class LoggerExtensions
    {
        public static void LogWebView2NavigationStarting(this ILogger logger, object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logger.Log(CreateLogLines("NavigationStarting",
            [
                $"{nameof(sender)}          = {sender}",
                $"{nameof(e)}               = {e}",
                $"{nameof(e.IsRedirected)}    = {e.IsRedirected}",
                $"{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                $"{nameof(e.NavigationId)}    = {e.NavigationId}",
                $"{nameof(e.NavigationKind)}  = {e.NavigationKind}",
                $"{nameof(e.Uri)}             = \"{e.Uri}\""
            ]));
        }

        public static void LogWebView2NavigationCompleted(this ILogger logger, object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logger.Log(CreateLogLines("NavigationCompleted",
            [
                $"{nameof(sender)}         = {sender}",
                $"{nameof(e)}              = {e}",
                $"{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                $"{nameof(e.IsSuccess)}      = {e.IsSuccess}",
                $"{nameof(e.NavigationId)}   = {e.NavigationId}",
                $"{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
            ]));
        }

        public static void LogWebView2DownloadStarting(this ILogger logger, object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logger.Log(CreateLogLines("DownloadStarting",
            [
                $"{nameof(sender)}                                = {sender}",
                $"{nameof(e)}                                     = {e}",
                $"{nameof(e.DownloadOperation.ResultFilePath)}      = \"{e.DownloadOperation.ResultFilePath}\"",
                $"{nameof(e.DownloadOperation.TotalBytesToReceive)} = {e.DownloadOperation.TotalBytesToReceive}",
                $"{nameof(e.DownloadOperation.Uri)}                 = \"{e.DownloadOperation.Uri}\""
            ]));
        }

        public static void LogWebView2StateChanged(this ILogger logger, object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                logger.Log(CreateLogLines("StateChanged",
                [
                    $"{nameof(sender)}                              = {sender}",
                    $"{nameof(e)}                                   = {e}",
                    $"{nameof(downloadOperation.BytesReceived)}       = {downloadOperation.BytesReceived}",
                    $"{nameof(downloadOperation.InterruptReason)}     = {downloadOperation.InterruptReason}",
                    $"{nameof(downloadOperation.ResultFilePath)}      = \"{downloadOperation.ResultFilePath}\"",
                    $"{nameof(downloadOperation.State)}               = {downloadOperation.State}",
                    $"{nameof(downloadOperation.TotalBytesToReceive)} = {downloadOperation.TotalBytesToReceive}",
                    $"{nameof(downloadOperation.Uri)}                 = \"{downloadOperation.Uri}\""
                ]));
            }
            else
            {
                logger.Log(CreateLogLines("StateChanged",
                [
                    $"{nameof(sender)} = {sender}",
                    $"{nameof(e)}      = {e}",
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
