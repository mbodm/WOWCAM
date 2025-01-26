using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Config
{
    public sealed class ConfigModule(ILogger logger, IConfigStorage storage, IConfigReader reader, IConfigValidator validator) : IConfigModule
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfigStorage storage = storage ?? throw new ArgumentNullException(nameof(storage));
        private readonly IConfigReader reader = reader ?? throw new ArgumentNullException(nameof(reader));
        private readonly IConfigValidator validator = validator ?? throw new ArgumentNullException(nameof(ConfigModule.validator));

        public ConfigData Data { get; private set; } = new ConfigData(string.Empty, string.Empty, [], string.Empty, []);

        public string StorageInformation => storage.StorageInformation;
        public bool StorageExists => storage.StorageExists;

        public async Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await storage.CreateStorageWithDefaultsAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not create empty default config (see log file for details).", e);
            }
        }

        public async Task LoadFromStorageAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Data = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not load config (see log file for details).", e);
            }
        }

        public void Validate()
        {
            try
            {
                validator.Validate(Data);
            }
            catch (Exception e)
            {
                if (e is ValidationException)
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
    }
}
