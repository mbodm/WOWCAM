namespace WOWCAM.Core
{
    public interface ISettings
    {
        string WorkFolder { get; }
        IEnumerable<string> Options { get; }

        string WebViewEnvironmentFolder { get; }
        string WebViewUserDataFolder { get; }

        IEnumerable<string> AddonUrls { get; }
        string AddonDownloadFolder { get; }
        string AddonUnzipFolder { get; }
        string AddonTargetFolder { get; }

        string AppUpdateFolder { get; }

        void Load();
    }
}
