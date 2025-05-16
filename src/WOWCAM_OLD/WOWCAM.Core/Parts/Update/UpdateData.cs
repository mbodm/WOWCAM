namespace WOWCAM.Core.Parts.Update
{
    public sealed record UpdateData(
        Version InstalledVersion,
        Version AvailableVersion,
        bool UpdateAvailable,
        string UpdateDownloadUrl,
        string UpdateFileName);
}
