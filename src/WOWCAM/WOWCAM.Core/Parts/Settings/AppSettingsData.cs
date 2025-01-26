namespace WOWCAM.Core.Parts.Settings
{
    public sealed record AppSettingsData(
        IEnumerable<string> Options,
        string WorkFolder,
        string WebViewEnvironmentFolder,
        string WebViewUserDataFolder,
        IEnumerable<string> AddonUrls,
        string AddonTargetFolder,
        string AddonDownloadFolder,
        string AddonUnzipFolder,
        string SmartUpdateFolder,
        string AppUpdateFolder);
}
