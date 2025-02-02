namespace WOWCAM.Core.Parts.Modules
{
    public sealed record SettingsData(
        IEnumerable<string> Options,
        string WorkFolder,
        string WebViewUserDataFolder,
        IEnumerable<string> AddonUrls,
        string AddonTargetFolder,
        string AddonDownloadFolder,
        string AddonUnzipFolder,
        string SmartUpdateFolder,
        string AppUpdateFolder)
    {
        public static SettingsData Empty()
        {
            return new(
                [],
                string.Empty,
                string.Empty,
                [],
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }
}
