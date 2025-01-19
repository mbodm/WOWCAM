using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultSettings(ILogger logger, IConfig config) : ISettings
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));

        public string WorkFolder { get; set; } = string.Empty;
        public IEnumerable<string> Options { get; set; } = [];

        public string WebViewEnvironmentFolder { get; set; } = string.Empty;
        public string WebViewUserDataFolder { get; set; } = string.Empty;

        public IEnumerable<string> AddonUrls { get; set; } = [];
        public string AddonDownloadFolder { get; set; } = string.Empty;
        public string AddonUnzipFolder { get; set; } = string.Empty;
        public string AddonTargetFolder { get; set; } = string.Empty;

        public string AppUpdateFolder { get; set; } = string.Empty;

        public void Load()
        {
            WorkFolder = Path.Combine(AppHelper.GetApplicationExecutableFolder(), "Internal");
            Options = config.ActiveOptions;

            WebViewEnvironmentFolder = Path.Combine(config.TempFolder, "MBODM-WOWCAM-WebView2-Env");
            WebViewUserDataFolder = Path.Combine(WorkFolder, "WebView2-UDF");

            AddonUrls = config.AddonUrls;
            AddonDownloadFolder = Path.Combine(WorkFolder, "AddonDownload");
            AddonUnzipFolder = Path.Combine(WorkFolder, "AddonUnzip");
            AddonTargetFolder = config.TargetFolder;

            AppUpdateFolder = Path.Combine(WorkFolder, "AppUpdate");

            logger.Log("Settings loaded.");

            logger.Log($"{nameof(WorkFolder)}: {WorkFolder}");
            logger.Log($"{nameof(Options)}: {string.Join(", ", Options)}");

            logger.Log($"{nameof(WebViewEnvironmentFolder)}: {WebViewEnvironmentFolder}");
            logger.Log($"{nameof(WebViewUserDataFolder)}: {WebViewUserDataFolder}");

            logger.Log($"{nameof(AddonUrls)}: {AddonUrls.Count()}");
            logger.Log($"{nameof(AddonDownloadFolder)}: {AddonDownloadFolder}");
            logger.Log($"{nameof(AddonUnzipFolder)}: {AddonUnzipFolder}");
            logger.Log($"{nameof(AddonTargetFolder)}: {AddonTargetFolder}");

            logger.Log($"{nameof(AppUpdateFolder)}: {AppUpdateFolder}");
        }
    }
}
