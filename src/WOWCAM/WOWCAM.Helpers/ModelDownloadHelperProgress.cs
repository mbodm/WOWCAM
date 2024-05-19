namespace WOWCAM.Helpers
{
    public sealed record ModelDownloadHelperProgress(
        string Url,
        bool IsPreDownloadSizeDetermination,
        long TotalBytes,
        long ReceivedBytes);
}
