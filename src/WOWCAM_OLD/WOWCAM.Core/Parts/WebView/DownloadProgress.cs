namespace WOWCAM.Core.Parts.WebView
{
    public record DownloadProgress(
        string DownloadUrl,
        string FilePath,
        uint TotalBytes,
        uint ReceivedBytes);
}
