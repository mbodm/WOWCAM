using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper.Parts;

namespace WOWCAM.Core.Parts.Settings
{
    public sealed class DefaultAppSettings(ILogger logger, IConfigStorage configStorage, IConfigReader configReader, IConfigValidator configValidator) : IAppSettings
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigStorage configStorage = configStorage ?? throw new ArgumentNullException(nameof(configStorage));
        private readonly IConfigReader configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
        private readonly IConfigValidator configValidator = configValidator ?? throw new ArgumentNullException(nameof(DefaultAppSettings.configValidator));

        public string ConfigStorageInformation => configStorage.StorageInformation;
        public SettingsData Data { get; private set; } = SettingsData.Empty();

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!configStorage.StorageExists)
            {
                try
                {
                    await configStorage.CreateStorageWithDefaultsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Log(e);
                    throw new InvalidOperationException("Could not create empty default config (see log file for details).", e);
                }
            }

            ConfigData configData;
            try
            {
                configData = await configReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not load config (see log file for details).", e);
            }

            try
            {
                configValidator.Validate(configData);
            }
            catch (Exception e)
            {
                if (e is ConfigValidationException)
                {
                    throw;
                }
                else
                {
                    logger.Log(e);
                    throw new InvalidOperationException("Data validation of loaded config failed (see log file for details).", e);
                }
            }

            var workFolder = AppHelper.GetApplicationExecutableFolder();
            Data = new SettingsData(
                Options: configData.ActiveOptions,
                WorkFolder: workFolder,
                WebViewUserDataFolder: Path.Combine(workFolder, "TempFolders", "WebView2-UDF"),
                AddonUrls: configData.AddonUrls,
                AddonTargetFolder: configData.TargetFolder,
                AddonDownloadFolder: Path.Combine(workFolder, "TempFolders", "Curse-Download"),
                AddonUnzipFolder: Path.Combine(workFolder, "TempFolders", "Curse-Unzip"),
                SmartUpdateFolder: Path.Combine(workFolder, "SmartUpdate"),
                AppUpdateFolder: Path.Combine(workFolder, "TempFolders", "App-Update"));

            var optionsAsString = Data.Options.Any() ? string.Join(", ", Data.Options) : "NONE";
            logger.Log(
            [
                "Application settings loaded",
                    $" => {nameof(Data.Options)}                  = {optionsAsString}",
                    $" => {nameof(Data.WorkFolder)}               = {Data.WorkFolder}",
                    $" => {nameof(Data.WebViewUserDataFolder)}    = {Data.WebViewUserDataFolder}",
                    $" => {nameof(Data.AddonUrls)}                = {Data.AddonUrls.Count()}",
                    $" => {nameof(Data.AddonTargetFolder)}        = {Data.AddonTargetFolder}",
                    $" => {nameof(Data.AddonDownloadFolder)}      = {Data.AddonDownloadFolder}",
                    $" => {nameof(Data.AddonUnzipFolder)}         = {Data.AddonUnzipFolder}",
                    $" => {nameof(Data.SmartUpdateFolder)}        = {Data.SmartUpdateFolder}",
                    $" => {nameof(Data.AppUpdateFolder)}          = {Data.AppUpdateFolder}"
            ]);
        }
    }
}
