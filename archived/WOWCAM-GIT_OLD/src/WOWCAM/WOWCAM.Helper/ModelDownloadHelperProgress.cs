namespace WOWCAM.Helper
{
    public sealed record ModelDownloadHelperProgress(
        string Url,
        bool PreTransfer,
        long ReceivedBytes,
        long TotalBytes,
        bool TransferFinished);
}
