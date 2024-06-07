namespace WOWCAM.Core
{
    public sealed record ModelApplicationUpdateData(
        Version InstalledVersion,
        Version AvailableVersion,
        bool UpdateAvailable,
        string UpdateDownloadUrl,
        string UpdateFileName);
}
