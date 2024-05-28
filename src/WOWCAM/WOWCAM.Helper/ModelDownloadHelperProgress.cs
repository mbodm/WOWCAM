namespace WOWCAM.Helper
{
    public sealed record ModelDownloadHelperProgress(
        string Url,
        bool IsPreDownloadSizeDetermination,
        long TotalBytes,
        long ReceivedBytes);
}
