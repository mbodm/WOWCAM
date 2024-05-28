using System.ComponentModel;
using System.Diagnostics;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(
        ILogger logger,
        IAppHelper appHelper,
        IFileSystemHelper fileSystemHelper,
        IGitHubHelper gitHubHelper,
        IConfig config,
        IDownloadHelper downloadHelper,
        IZipFileHelper zipFileHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));

        private readonly string appName = appHelper.GetApplicationName();

        private string updateFolder = string.Empty;

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
                throw new InvalidOperationException($"Could not determine the latest {appName} version on GitHub (see log file for details).", e);
            }
        }

        public async Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            // Prepare

            try
            {
                // Trust application and config validator (since this is business logic and not a helper) and therefore do no temp folder check here

                updateFolder = Path.Combine(config.TempFolder, "MBODM-WOWCAM-Update");

                if (Directory.Exists(updateFolder))
                {
                    Directory.Delete(updateFolder, true);
                }

                Directory.CreateDirectory(updateFolder);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while application prepares for download (see log file for details).", e);
            }

            // Download

            try
            {
                var downloadUrl = updateData.UpdateDownloadUrl;
                var downloadFilePath = Path.Combine(updateFolder, updateData.UpdateFileName);

                if (File.Exists(downloadFilePath))
                {
                    File.Delete(downloadFilePath);
                }

                await downloadHelper.DownloadFileAsync(downloadUrl, downloadFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Error while downloading latest {appName} release zip file (see log file for details).", e);
            }
        }

        public async Task<bool> ApplyUpdateAsync(CancellationToken cancellationToken = default)
        {
            // Prepare for update

            var installedVersion = GetInstalledVersion();

            try
            {
                var updateZipFile = Directory.EnumerateFiles(updateFolder, "*.zip").FirstOrDefault() ??
                    throw new InvalidOperationException("Update folder not contains zip file.");

                await zipFileHelper.ExtractZipFileAsync(updateZipFile, updateFolder, cancellationToken).ConfigureAwait(false);

                var appFileName = appHelper.GetApplicationExecutableFileName();

                var updateExeFile = Path.Combine(updateFolder, appFileName);
                if (!File.Exists(updateExeFile))
                    throw new InvalidOperationException($"Extracted zip file, but update folder not contains {appFileName} file.");

                var updateVersion = fileSystemHelper.GetExeFileVersion(updateExeFile);
                if (updateVersion < installedVersion)
                    throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while application prepares for update (see log file for details).", e);
            }

            // Start external update app with admin rights

            try
            {
                var updateAppFileName = "wcupdate.exe";
                var updateAppFilePath = Path.Combine(appHelper.GetApplicationExecutableFolder(), updateAppFileName);

                if (!File.Exists(updateAppFilePath))
                    throw new InvalidOperationException($"Could not found {updateAppFileName} in application folder.");

                // See StackOverflow:
                // https://stackoverflow.com/questions/16926232/run-process-as-administrator-from-a-non-admin-application
                // https://stackoverflow.com/questions/3925065/correct-way-to-deal-with-uac-in-c-sharp

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = updateAppFilePath,
                    Arguments = $"{updateFolder} {Environment.ProcessId}",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                if (Process.Start(processStartInfo) == null)
                    throw new InvalidOperationException("The 'Process.Start()' call returned null.");

                return true;
            }
            catch (Exception e)
            {
                if (e is Win32Exception win32Exception && win32Exception.NativeErrorCode == 1223)
                {
                    logger.Log("User cancelled Windows UAC popup while application update process.");
                    return false;
                }

                logger.Log(e);
                throw new InvalidOperationException("Error while starting update app (see log file for details).", e);
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
    }
}
