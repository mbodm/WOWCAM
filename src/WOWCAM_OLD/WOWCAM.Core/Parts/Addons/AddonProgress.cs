namespace WOWCAM.Core.Parts.Addons
{
    public sealed record AddonProgress(
        AddonState AddonState,
        string AddonName,
        byte DownloadPercent);
}
