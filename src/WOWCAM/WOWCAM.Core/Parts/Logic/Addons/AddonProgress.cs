namespace WOWCAM.Core.Parts.Logic.Addons
{
    public sealed record AddonProgress(
        AddonState AddonState,
        string AddonName,
        byte DownloadPercent);
}
