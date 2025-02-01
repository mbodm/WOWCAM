namespace WOWCAM.Core.Parts.Config
{
    public sealed record AppSettings(
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
        public static AppSettings Empty()
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
