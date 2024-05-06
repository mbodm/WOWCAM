namespace WOWCAM.Core
{
    public sealed class XmlFileConfigValidator(ILogger logger, IConfig config, ICurseHelper curseHelper) : IConfigValidator
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

        public void Validate()
        {
            try
            {
                if (config.TargetFolder == string.Empty)
                {
                    throw new InvalidOperationException("Config file contains no target folder to download and extract the zip files into.");
                }

                if (!IsValidAbsolutePath(config.TargetFolder))
                {
                    throw new InvalidOperationException("Config file contains a target folder which is not a valid absolute file system path.");
                }

                if (!config.AddonUrls.Any())
                {
                    throw new InvalidOperationException("Config file contains 0 addon url entries and so there is nothing to download.");
                }

                if (config.AddonUrls.Any(url => !curseHelper.IsAddonPageUrl(url)))
                {
                    throw new InvalidOperationException("Config file contains at least 1 addon url entry which is not a valid Curse addon url.");
                }
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw;
            }
        }

        private static bool IsValidAbsolutePath(string path)
        {
            try
            {
                if (!Path.IsPathRooted(path))
                {
                    return false;
                }

                Path.GetFullPath(path);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
