using WOWCAM.Core.Parts.Logic.Config;
using WOWCAM.Helper.Parts.Application;
using WOWCAM.Logging;

namespace WOWCAM.Core.Parts.Modules
{
    public sealed class DefaultSettingsModule(ILogger logger, IConfigStorage configStorage, IConfigReader configReader, IConfigValidator configValidator) : ISettingsModule
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigStorage configStorage = configStorage ?? throw new ArgumentNullException(nameof(configStorage));
        private readonly IConfigReader configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
        private readonly IConfigValidator configValidator = configValidator ?? throw new ArgumentNullException(nameof(DefaultSettingsModule.configValidator));

        public string StorageInformation => configStorage.StorageInformation;
        public SettingsData SettingsData { get; private set; } = SettingsData.Empty();

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
            var settingsData = new SettingsData(
                Options: configData.ActiveOptions,
                WorkFolder: workFolder,
                TempFolder: Path.Combine(workFolder, "TempFolders"),
                AddonUrls: configData.AddonUrls,
                AddonTargetFolder: configData.TargetFolder);

            var optionsAsString = settingsData.Options.Any() ? string.Join(", ", settingsData.Options) : "NONE";
            logger.Log(
            [
                "Application settings loaded",
                    $" => {nameof(SettingsData.Options)}           = {optionsAsString}",
                    $" => {nameof(SettingsData.WorkFolder)}        = {settingsData.WorkFolder}",
                    $" => {nameof(SettingsData.TempFolder)}        = {settingsData.TempFolder}",
                    $" => {nameof(SettingsData.AddonUrls)}         = {settingsData.AddonUrls.Count()}",
                    $" => {nameof(SettingsData.AddonTargetFolder)} = {settingsData.AddonTargetFolder}",
            ]);

            SettingsData = settingsData;
        }
    }
}
