namespace WOWCAM.Core.Parts.Settings
{
    public sealed record AppSettingsData(
        string WorkFolder,
        IEnumerable<string> Options,
        string WebViewEnvironmentFolder,
        string WebViewUserDataFolder,
        IEnumerable<string> AddonUrls,
        string AddonDownloadFolder,
        string AddonWorkFolder,
        string AddonTargetFolder,
        string AppUpdateFolder);
}
