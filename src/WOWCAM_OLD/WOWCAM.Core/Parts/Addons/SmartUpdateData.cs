namespace WOWCAM.Core.Parts.Addons
{
    public sealed record SmartUpdateData(
        string AddonName,
        string DownloadUrl,
        string ZipFile,
        string TimeStamp);
}
