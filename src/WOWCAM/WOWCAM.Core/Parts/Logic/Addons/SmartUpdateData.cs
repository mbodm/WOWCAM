namespace WOWCAM.Core.Parts.Logic.Addons
{
    public sealed record SmartUpdateData(
        string AddonName,
        string DownloadUrl,
        string ZipFile,
        string TimeStamp);
}
