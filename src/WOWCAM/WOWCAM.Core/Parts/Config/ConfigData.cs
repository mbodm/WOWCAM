namespace WOWCAM.Core.Parts.Config
{
    public sealed record ConfigData(
        string ActiveProfile,
        string TempFolder,
        IEnumerable<string> ActiveOptions,
        string TargetFolder,
        IEnumerable<string> AddonUrls);
}
