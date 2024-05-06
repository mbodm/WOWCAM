using Microsoft.Web.WebView2.Core;

namespace WOWCAM.WebView
{
    public static class LogHelper
    {
        public static IEnumerable<string> CreateLines(string name, object? sender, object? e, IEnumerable<string> details)
        {
            var lines = new string[]
            {
                $"{name} (event)",
                $"{nameof(sender)} = {sender}",
                $"{nameof(e)} = {e}"
            };

            return lines.Concat(details);
        }

        public static IEnumerable<string> GetDownloadOperationDetails(object? o)
        {
            if (o is CoreWebView2DownloadOperation downloadOperation)
            {
                return
                [
                    $"{nameof(downloadOperation.BytesReceived)} = {downloadOperation.BytesReceived}",
                    $"{nameof(downloadOperation.CanResume)} = {downloadOperation.CanResume}",
                    $"{nameof(downloadOperation.ContentDisposition)} = \"{downloadOperation.ContentDisposition}\"",
                    $"{nameof(downloadOperation.EstimatedEndTime)} = {downloadOperation.EstimatedEndTime}",
                    $"{nameof(downloadOperation.InterruptReason)} = {downloadOperation.InterruptReason}",
                    $"{nameof(downloadOperation.MimeType)} = \"{downloadOperation.MimeType}\"",
                    $"{nameof(downloadOperation.ResultFilePath)} = \"{downloadOperation.ResultFilePath}\"",
                    $"{nameof(downloadOperation.State)} = {downloadOperation.State}",
                    $"{nameof(downloadOperation.TotalBytesToReceive)} = {downloadOperation.TotalBytesToReceive}",
                    $"{nameof(downloadOperation.Uri)} = \"{downloadOperation.Uri}\""
                ];
            }

            return [];
        }
    }
}
