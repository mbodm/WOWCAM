using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Settings
{
    public sealed class DefaultAppSettings(ILogger logger, IConfig config) : IAppSettings
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));

        private bool isInitialized;

        public AppSettingsData Data { get; private set; } = new AppSettingsData(
            string.Empty, [], string.Empty, string.Empty, [], string.Empty, string.Empty, string.Empty, string.Empty);

        public void Init()
        {
            if (!isInitialized)
            {
                var workFolder = Path.Combine(AppHelper.GetApplicationExecutableFolder(), "Temp");
                var options = config.Data.ActiveOptions;

                var webViewEnvironmentFolder = Path.Combine(config.Data.TempFolder, "MBODM-WOWCAM-WebView2-Env");
                var webViewUserDataFolder = Path.Combine(workFolder, "WebView2-UDF");

                var addonUrls = config.Data.AddonUrls;
                var addonDownloadFolder = Path.Combine(workFolder, "AddonDownload");
                var addonUnzipFolder = Path.Combine(workFolder, "AddonUnzip");
                var addonTargetFolder = config.Data.TargetFolder;

                var appUpdateFolder = Path.Combine(workFolder, "AppUpdate");

                Data = new AppSettingsData(
                    workFolder, options, webViewEnvironmentFolder, webViewUserDataFolder, addonUrls, addonDownloadFolder, addonUnzipFolder, addonTargetFolder, appUpdateFolder);

                var optionsAsString = Data.Options.Any() ? string.Join(", ", Data.Options) : "NONE";
                var messages = new List<string>
                {
                    "Application-Settings initialized:",
                    $"{nameof(Data.WorkFolder)}                ->  {Data.WorkFolder}",
                    $"{nameof(Data.Options)}                   ->  {optionsAsString}",
                    $"{nameof(Data.WebViewEnvironmentFolder)}  ->  {Data.WebViewEnvironmentFolder}",
                    $"{nameof(Data.WebViewUserDataFolder)}     ->  {Data.WebViewUserDataFolder}",
                    $"{nameof(Data.AddonUrls)}                 ->  {Data.AddonUrls.Count()}",
                    $"{nameof(Data.AddonDownloadFolder)}       ->  {Data.AddonDownloadFolder}",
                    $"{nameof(Data.AddonWorkFolder)}          ->  {Data.AddonWorkFolder}",
                    $"{nameof(Data.AddonTargetFolder)}         ->  {Data.AddonTargetFolder}",
                    $"{nameof(Data.AppUpdateFolder)}           ->  {Data.AppUpdateFolder}"
                };

                logger.Log(messages);

                isInitialized = true;
            }
        }
    }
}
