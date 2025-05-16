namespace WOWCAM.Helper.Parts.Download
{
    public sealed record DownloadProgress(
        string Url,
        bool PreTransfer,
        long ReceivedBytes,
        long TotalBytes,
        bool TransferFinished);
}
