using System.Diagnostics;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(
        ILogger logger,
        IAppHelper appHelper,
        IGitHubHelper gitHubHelper,
        IConfig config,
        IFileSystemHelper fileSystemHelper,
        IDownloadHelper downloadHelper,
        IZipFileHelper zipFileHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));

        private readonly string appName = appHelper.GetApplicationName();
        private readonly string appFileName = appHelper.GetApplicationExecutableFileName();

        public async Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();

            try
            {
                var latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);
                var updateAvailable = installedVersion < latestReleaseData.Version;

                return new ModelApplicationUpdateData(installedVersion, latestReleaseData.Version, updateAvailable, latestReleaseData.DownloadUrl, latestReleaseData.FileName);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Could not determine the latest {appName} version (see log file for details).", e);
            }
        }

        public async Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var updateFolder = GetUpdateFolder();
                var releaseZipFilePath = Path.Combine(updateFolder, updateData.UpdateFileName);

                if (!Directory.Exists(updateFolder))
                {
                    Directory.CreateDirectory(updateFolder);
                }
                else
                {
                    await fileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
                }

                await downloadHelper.DownloadFileAsync(updateData.UpdateDownloadUrl, releaseZipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);

                if (!File.Exists(releaseZipFilePath))
                    throw new InvalidOperationException("Downloaded latest release, but update folder not contains zip file.");

                await zipFileHelper.ExtractZipFileAsync(releaseZipFilePath, updateFolder, cancellationToken).ConfigureAwait(false);

                if (!File.Exists(Path.Combine(updateFolder, appFileName)))
                    throw new InvalidOperationException($"Extracted zip file, but update folder not contains {appFileName} file.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Error while downloading {appName} release (see log file for details).", e);
            }
        }

        public void ApplyUpdate()
        {
            // Prepare for update

            var installedVersion = GetInstalledVersion();
            var updateFolder = GetUpdateFolder();

            try
            {
                if (!Directory.Exists(updateFolder))
                    throw new InvalidOperationException("Update folder not exists.");

                var newExeFilePath = Path.Combine(updateFolder, appFileName);
                if (!File.Exists(newExeFilePath))
                    throw new InvalidOperationException($"Update folder not contains {appFileName} file.");

                var newExeVersion = fileSystemHelper.GetExeFileVersion(newExeFilePath);
                if (newExeVersion < installedVersion)
                    throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while application prepares for update (see log file for details).", e);
            }

            // Start update tool

            try
            {
                var updateToolFileName = "wcupdate.exe";
                var updateToolFilePath = Path.Combine(appHelper.GetApplicationExecutableFolder(), updateToolFileName);

                if (!File.Exists(updateToolFilePath))
                    throw new InvalidOperationException($"Could not found {updateToolFileName} in application folder.");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = updateToolFilePath,
                    Arguments = $"{updateFolder} {Environment.ProcessId}",
                    UseShellExecute = true,
                };

                if (Process.Start(processStartInfo) == null)
                    throw new InvalidOperationException("The 'Process.Start()' call returned null.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while starting update tool (see log file for details).", e);
            }
        }

        private Version GetInstalledVersion()
        {
            try
            {
                var installedExeFile = appHelper.GetApplicationExecutableFilePath();
                var installedVersion = fileSystemHelper.GetExeFileVersion(installedExeFile);

                return installedVersion;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Could not determine installed {appName} version (see log file for details).", e);
            }
        }

        private string GetUpdateFolder()
        {
            // Trust application and config validator (since this is business logic and not a helper) and therefore do no temp folder check here

            return Path.Combine(config.TempFolder, "MBODM-WOWCAM-Update");
        }
    }
}
