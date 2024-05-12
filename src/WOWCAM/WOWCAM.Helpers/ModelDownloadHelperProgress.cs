namespace WOWCAM.Helpers
{
    public sealed record ModelDownloadHelperProgress(
        string Url,
        long ReceivedBytes,
        long TotalBytes);
}
