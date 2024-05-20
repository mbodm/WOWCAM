using System.Diagnostics;
using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(
        ILogger logger,
        IConfig config,
        IAppHelper appHelper,
        IGitHubHelper gitHubHelper,
        IFileSystemHelper fileSystemHelper,
        IDownloadHelper downloadHelper,
        IZipFileHelper zipFileHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));

        public async Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            Version installedVersion;

            try
            {
                var installedExeFile = Path.Combine(appHelper.GetApplicationExecutableFolder(), "WOWCAM.exe");

                installedVersion = fileSystemHelper.GetExeFileVersion(installedExeFile);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the installed WOWCAM version (see log file for details).", e);
            }

            ModelGitHubLatestReleaseData latestReleaseData;

            try
            {
                latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }

            var updateAvailable = installedVersion < latestReleaseData.Version;

            return new ModelApplicationUpdateData(installedVersion, latestReleaseData.Version, updateAvailable, latestReleaseData.DownloadUrl, latestReleaseData.FileName);
        }

        public async Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            var downloadUrl = updateData.UpdateDownloadUrl;
            var downloadFolder = GetUpdateFolder();
            var filePath = Path.Combine(downloadFolder, updateData.UpdateFileName);

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                await downloadHelper.DownloadFileAsync(downloadUrl, filePath, downloadProgress, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while downloading latest WOWCAM release zip file from GitHub (see log file for details).", e);
            }
        }

        public async Task StartUpdaterWithAdminRightsAsync(Action restartApplicationAction)
        {
            var updaterExe = Path.Combine(appHelper.GetApplicationExecutableFolder(), "WOWCAM.Update.exe");

            if (!File.Exists(updaterExe))
            {
                throw new InvalidOperationException("cowa");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = updaterExe,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                if (Process.Start(processStartInfo) == null)
                {
                    throw new InvalidOperationException("The 'Process.Start()' call returned null.");
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }

            await Task.Delay(500);
            restartApplicationAction?.Invoke();
            await Task.Delay(500);
        }

        public async Task SelfUpdateIfRequestedAsync(Action restartApplicationAction, CancellationToken cancellationToken = default)
        {
            try
            {
                var updateFolder = GetUpdateFolder();

                if (!Directory.Exists(updateFolder))
                {
                    Directory.CreateDirectory(updateFolder);
                }

                var updateFile = Directory.EnumerateFiles(updateFolder, "*.zip").FirstOrDefault();
                var exeFolder = appHelper.GetApplicationExecutableFolder();
                var exeFile = Path.Combine(exeFolder, "WOWCAM.exe");
                var exeFileRenamed = Path.Combine(exeFolder, "WOWCAM_exe_old.tmp");

                /*
                if (appHelper.ApplicationHasAdminRights() && updateFile != null)
                {
                    File.Move(exeFile, exeFileRenamed, true);

                    await zipFileHelper.ExtractZipFileAsync(updateFile, exeFolder, cancellationToken).ConfigureAwait(false);

                    if (!File.Exists(exeFile))
                    {
                        throw new InvalidOperationException("Could not found application executable.");
                    }

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(appHelper.GetApplicationExecutableFolder(), "WOWCAM.exe"),
                        UseShellExecute = true,
                    };

                    if (Process.Start(processStartInfo) == null)
                    {
                        throw new InvalidOperationException("The 'Process.Start()' call returned null.");
                    }

                    await Task.Delay(500);
                    restartApplicationAction?.Invoke();
                    await Task.Delay(500);
                
                }
                */
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Found application update, but could not apply update (see log file for details).", e);
            }

        }

        private string GetUpdateFolder()
        {
            // Trust application and config validator (since this is business logic and not a helper) and therefore do no temp folder check here

            return Path.Combine(config.TempFolder, "MBODM-WOWCAM-Update");
        }
    }
}
