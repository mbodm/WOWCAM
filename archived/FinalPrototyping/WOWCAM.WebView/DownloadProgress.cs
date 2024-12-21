namespace WOWCAM.WebView
{
    public record DownloadProgress(string DownloadUrl, string FilePath, ulong TotalBytesToReceive, long BytesReceived);
}
