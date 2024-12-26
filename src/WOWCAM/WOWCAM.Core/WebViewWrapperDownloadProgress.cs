namespace WOWCAM.Core
{
    public record WebViewWrapperDownloadProgress(string DownloadUrl, string FilePath, uint TotalBytes, uint ReceivedBytes);
}
