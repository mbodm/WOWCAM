using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultConfigValidator(ILogger logger, IConfig config, IFileSystemHelper fileSystemHelper, ICurseHelper curseHelper) : IConfigValidator
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));

        // See details and reasons for MaxPathLength value at
        // https://stackoverflow.com/questions/265769/maximum-filename-length-in-ntfs-windows-xp-and-windows-vista
        // https://stackoverflow.com/questions/23588944/better-to-check-if-length-exceeds-max-path-or-catch-pathtoolongexception

        private const int MaxPathLength = 240;

        public void Validate()
        {
            try
            {
                if (config.TempFolder == string.Empty)
                {
                    throw new InvalidOperationException("Config file contains no temp folder and also the application's own default fallback value (%TEMP%) is not active.");
                }

                ValidateFolder(config.TempFolder, "temp", MaxPathLength);

                if (config.TargetFolder == string.Empty)
                {
                    throw new InvalidOperationException("Config file contains no target folder to download and extract the zip files into.");
                }

                // Easy to foresee max length of temp. Not that easy to foresee max length of target, when considering content of
                // zip file (files and subfolders). Therefore just using half of MAX_PATH here, as some "rule of thumb". If in a
                // rare case a full dest path exceeds MAX_PATH, it seems ok to let the unzip operation fail gracefully on its own.

                ValidateFolder(config.TargetFolder, "target", MaxPathLength / 2);

                if (!config.AddonUrls.Any())
                {
                    throw new InvalidOperationException("Config file contains 0 addon URL entries and so there is nothing to download.");
                }

                if (config.AddonUrls.Any(url => !curseHelper.IsAddonPageUrl(url)))
                {
                    throw new InvalidOperationException("Config file contains at least 1 addon URL entry which is not a valid Curse addon URL.");
                }
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw;
            }
        }

        private void ValidateFolder(string folderValue, string folderName, int maxChars)
        {
            if (!fileSystemHelper.IsValidAbsolutePath(folderValue))
            {
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder which is not a valid folder path (given path must be a valid absolute path to a folder).");
            }

            if (folderValue.Length > maxChars)
            {
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder path which is too long (make sure given path is smaller than {maxChars} characters).");
            }

            if (!Directory.Exists(folderValue))
            {
                throw new InvalidOperationException(
                    $"Config file contains a {folderName} folder which not exists (the app not creates configured folders automatically, on purpose).");
            }
        }
    }
}
