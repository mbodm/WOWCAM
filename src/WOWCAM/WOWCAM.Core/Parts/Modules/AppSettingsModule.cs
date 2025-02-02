using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Modules
{
    public sealed class DefaultAppSettings(ILogger logger, IConfigStorage storage, IConfigReader reader, IConfigValidator validator) : IAppSettings
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigStorage storage = storage ?? throw new ArgumentNullException(nameof(storage));
        private readonly IConfigReader reader = reader ?? throw new ArgumentNullException(nameof(reader));
        private readonly IConfigValidator validator = validator ?? throw new ArgumentNullException(nameof(DefaultAppSettings.validator));

        public string StorageInformation => storage.StorageInformation;
        public SettingsData AppSettings { get; private set; } = AppSettings.Empty();

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!storage.StorageExists)
            {
                await CreateStorageWithDefaultsAsync(cancellationToken).ConfigureAwait(false);
            }

            var configData = await LoadFromStorageAsync(cancellationToken).ConfigureAwait(false);

            Validate(configData);

            try
            {
                var workFolder = AppHelper.GetApplicationExecutableFolder();

                AppSettings = new AppSettings(
                    Options: configData.ActiveOptions,
                    WorkFolder: workFolder,
                    WebViewUserDataFolder: Path.Combine(workFolder, "Temp", "WebView2-UDF"),
                    AddonUrls: configData.AddonUrls,
                    AddonTargetFolder: configData.TargetFolder,
                    AddonDownloadFolder: Path.Combine(workFolder, "Temp", "Curse-Download"),
                    AddonUnzipFolder: Path.Combine(workFolder, "Temp", "Curse-Unzip"),
                    SmartUpdateFolder: Path.Combine(workFolder, "SmartUpdate"),
                    AppUpdateFolder: Path.Combine(workFolder, "Temp", "App-Update"));

                var optionsAsString = AppSettings.Options.Any() ? string.Join(", ", AppSettings.Options) : "NONE";
                logger.Log(
                [
                    "Application settings loaded",
                    $" => {nameof(AppSettings.Options)}                  = {optionsAsString}",
                    $" => {nameof(AppSettings.WorkFolder)}               = {AppSettings.WorkFolder}",
                    $" => {nameof(AppSettings.WebViewUserDataFolder)}    = {AppSettings.WebViewUserDataFolder}",
                    $" => {nameof(AppSettings.AddonUrls)}                = {AppSettings.AddonUrls.Count()}",
                    $" => {nameof(AppSettings.AddonTargetFolder)}        = {AppSettings.AddonTargetFolder}",
                    $" => {nameof(AppSettings.AddonDownloadFolder)}      = {AppSettings.AddonDownloadFolder}",
                    $" => {nameof(AppSettings.AddonUnzipFolder)}         = {AppSettings.AddonUnzipFolder}",
                    $" => {nameof(AppSettings.SmartUpdateFolder)}        = {AppSettings.SmartUpdateFolder}",
                    $" => {nameof(AppSettings.AppUpdateFolder)}          = {AppSettings.AppUpdateFolder}"
                ]);

                await CreateFolderStructureAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error occurred while loading application settings (see log file for details).", e);
            }
        }

        private async Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await storage.CreateStorageWithDefaultsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not create empty default config (see log file for details).", e);
            }
        }

        private async Task<ConfigData> LoadFromStorageAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not load config (see log file for details).", e);
            }
        }

        private void Validate(ConfigData configData)
        {
            try
            {
                validator.Validate(configData);
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
        }

        private async Task CreateFolderStructureAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(AppSettings.WorkFolder))
            {
                Directory.CreateDirectory(AppSettings.WorkFolder);
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);

            if (!Directory.Exists(AppSettings.WebViewUserDataFolder))
            {
                Directory.CreateDirectory(AppSettings.WebViewUserDataFolder);
            }

            if (!Directory.Exists(AppSettings.AddonDownloadFolder))
            {
                Directory.CreateDirectory(AppSettings.AddonDownloadFolder);
            }

            if (!Directory.Exists(AppSettings.AddonUnzipFolder))
            {
                Directory.CreateDirectory(AppSettings.AddonUnzipFolder);
            }

            if (!Directory.Exists(AppSettings.AppUpdateFolder))
            {
                Directory.CreateDirectory(AppSettings.AppUpdateFolder);
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }
}
