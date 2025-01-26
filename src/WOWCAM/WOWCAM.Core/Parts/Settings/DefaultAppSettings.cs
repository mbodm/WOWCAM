using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Settings
{
    public sealed class DefaultAppSettings(ILogger logger, IConfigModule configModule) : IAppSettings
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigModule configModule = configModule ?? throw new ArgumentNullException(nameof(configModule));

        private bool isInitialized;

        public AppSettingsData Data { get; private set; } =
            new([], string.Empty, string.Empty, string.Empty, [], string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        public async Task InitAsync(CancellationToken cancellationToken = default)
        {
            if (!isInitialized)
            {
                var workFolder = Path.Combine(AppHelper.GetApplicationExecutableFolder(), "MBODM-WOWCAM-Internal");

                Data = new AppSettingsData(
                    Options: configModule.Data.ActiveOptions,
                    WorkFolder: workFolder,
                    WebViewEnvironmentFolder: Path.Combine(configModule.Data.TempFolder, "MBODM-WOWCAM-WebView2-Env"),
                    WebViewUserDataFolder: Path.Combine(workFolder, "WebView2-UDF"),
                    AddonUrls: configModule.Data.AddonUrls,
                    AddonTargetFolder: configModule.Data.TargetFolder,
                    AddonDownloadFolder: Path.Combine(workFolder, "Curse-Download"),
                    AddonUnzipFolder: Path.Combine(workFolder, "Curse-Unzip"),
                    SmartUpdateFolder: Path.Combine(workFolder, "SmartUpdate"),
                    AppUpdateFolder: Path.Combine(workFolder, "AppUpdate"));

                var optionsAsString = Data.Options.Any() ? string.Join(", ", Data.Options) : "NONE";
                logger.Log(
                [
                    "Application-Settings initialized:",
                    $"   {nameof(Data.Options)}                   ->  {optionsAsString}",
                    $"   {nameof(Data.WorkFolder)}                ->  {Data.WorkFolder}",
                    $"   {nameof(Data.WebViewEnvironmentFolder)}  ->  {Data.WebViewEnvironmentFolder}",
                    $"   {nameof(Data.WebViewUserDataFolder)}     ->  {Data.WebViewUserDataFolder}",
                    $"   {nameof(Data.AddonUrls)}                 ->  {Data.AddonUrls.Count()}",
                    $"   {nameof(Data.AddonTargetFolder)}         ->  {Data.AddonTargetFolder}",
                    $"   {nameof(Data.AddonDownloadFolder)}       ->  {Data.AddonDownloadFolder}",
                    $"   {nameof(Data.AddonUnzipFolder)}          ->  {Data.AddonUnzipFolder}",
                    $"   {nameof(Data.AppUpdateFolder)}           ->  {Data.AppUpdateFolder}"
                ]);

                //await CreateFolderStructureAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(1);

                isInitialized = true;
            }
        }

        private async Task CreateFolderStructureAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(Data.WorkFolder))
            {
                Directory.CreateDirectory(Data.WorkFolder);
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            if (!Directory.Exists(Data.WebViewEnvironmentFolder))
            {
                Directory.CreateDirectory(Data.WebViewEnvironmentFolder);
            }

            if (!Directory.Exists(Data.WebViewUserDataFolder))
            {
                Directory.CreateDirectory(Data.WebViewUserDataFolder);
            }

            if (!Directory.Exists(Data.AddonDownloadFolder))
            {
                Directory.CreateDirectory(Data.AddonDownloadFolder);
            }

            if (!Directory.Exists(Data.AddonUnzipFolder))
            {
                Directory.CreateDirectory(Data.AddonUnzipFolder);
            }

            if (!Directory.Exists(Data.AppUpdateFolder))
            {
                Directory.CreateDirectory(Data.AppUpdateFolder);
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }
}
