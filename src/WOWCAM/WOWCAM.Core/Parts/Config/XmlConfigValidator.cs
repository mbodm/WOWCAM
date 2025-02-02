using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Config
{
    public sealed class XmlConfigValidator(ILogger logger) : IConfigValidator
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void Validate(ConfigData configData)
        {
            logger.LogMethodEntry();

            ArgumentNullException.ThrowIfNull(configData);

            // See details and reasons for MaxPathLength value at:
            // https://stackoverflow.com/questions/265769/maximum-filename-length-in-ntfs-windows-xp-and-windows-vista
            // https://stackoverflow.com/questions/23588944/better-to-check-if-length-exceeds-max-path-or-catch-pathtoolongexception

            const int MaxPathLength = 240;

            if (string.IsNullOrWhiteSpace(configData.TempFolder))
            {
                throw new ConfigValidationException("Config file contains no temp folder (not even the application's default fallback value of %TEMP% is there).");
            }

            ValidateFolder(configData.TempFolder, "temp", MaxPathLength);

            if (string.IsNullOrWhiteSpace(configData.TargetFolder))
            {
                throw new ConfigValidationException("Config file contains no target folder to download and extract the zip files into.");
            }

            // Easy to foresee max length of temp. Not that easy to foresee max length of target, when considering content of
            // zip file (files and subfolders). Therefore just using half of MAX_PATH here, as some "rule of thumb". If in a
            // rare case a full dest path exceeds MAX_PATH, it seems OK to let the unzip operation fail gracefully on its own.

            ValidateFolder(configData.TargetFolder, "target", MaxPathLength / 2);

            if (!configData.AddonUrls.Any())
            {
                throw new ConfigValidationException("Config file contains 0 addon URL entries and so there is nothing to download.");
            }

            if (configData.AddonUrls.Any(url => !CurseHelper.IsAddonPageUrl(url)))
            {
                throw new ConfigValidationException("Config file contains at least 1 addon URL entry which is not a valid Curse addon URL.");
            }

            logger.LogMethodExit();
        }

        private static void ValidateFolder(string folderValue, string folderName, int maxChars)
        {
            if (!FileSystemHelper.IsValidAbsolutePath(folderValue))
            {
                throw new ConfigValidationException(
                    $"Config file contains a {folderName} folder which is not a valid folder path (given path must be a valid absolute path to a folder).");
            }

            if (folderValue.Length > maxChars)
            {
                throw new ConfigValidationException(
                    $"Config file contains a {folderName} folder path which is too long (make sure given path is smaller than {maxChars} characters).");
            }

            // I decided to NOT create any configured folder by code since the default config makes various assumptions (i.e. WoW's location in %PROGRAMFILES(X86)% folder)

            if (!Directory.Exists(folderValue))
            {
                throw new ConfigValidationException(
                    $"Config file contains a {folderName} folder which not exists (the app will not create any configured folder automatically, on purpose).");
            }
        }
    }
}
