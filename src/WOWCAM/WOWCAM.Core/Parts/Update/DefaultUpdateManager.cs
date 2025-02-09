using System.Diagnostics;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.System;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Update
{
    public sealed class DefaultUpdateManager(ILogger logger, IAppSettings appSettings, IReliableFileOperations reliableFileOperations, HttpClient httpClient) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        private readonly string appName = AppHelper.GetApplicationName();
        private readonly string appFileName = AppHelper.GetApplicationExecutableFileName();

        public async Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();

            try
            {
                var latestReleaseData = await GitHubHelper.GetLatestReleaseDataAsync("mbodm", "wowcam", httpClient, cancellationToken).ConfigureAwait(false);
                var updateAvailable = installedVersion < latestReleaseData.Version;

                return new UpdateData(installedVersion, latestReleaseData.Version, updateAvailable, latestReleaseData.DownloadUrl, latestReleaseData.FileName);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Could not determine the latest {appName} version (see log file for details).", e);
            }
        }

        public async Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var updateFolder = GetUpdateFolder();
                if (!Directory.Exists(updateFolder))
                {
                    Directory.CreateDirectory(updateFolder);
                }
                else
                {
                    await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
                }

                var zipFilePath = Path.Combine(updateFolder, updateData.UpdateFileName);
                await DownloadHelper.DownloadFileAsync(httpClient, updateData.UpdateDownloadUrl, zipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);
                if (!File.Exists(zipFilePath))
                {
                    throw new InvalidOperationException("Downloaded latest release, but update folder not contains zip file.");
                }

                await UnzipHelper.ExtractZipFileAsync(zipFilePath, updateFolder, cancellationToken).ConfigureAwait(false);
                var newExeFilePath = Path.Combine(updateFolder, appFileName);
                if (!File.Exists(newExeFilePath))
                {
                    throw new InvalidOperationException($"Extracted zip file, but update folder not contains {appFileName} file.");
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Error while downloading latest {appName} release (see log file for details).", e);
            }
        }

        public Task ApplyUpdateAsync(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();
            var newExeFilePath = PrepareForUpdateAndReturnNewExeFilePath(installedVersion);

            try
            {
                var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
                var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

                File.Move(exeFilePath, bakFilePath, true);
                File.Copy(newExeFilePath, exeFilePath, true);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not apply update (see log file for details).", e);
            }

            return reliableFileOperations.WaitAsync(cancellationToken);
        }

        public void RestartApplication(uint delayInSeconds)
        {
            if (delayInSeconds > 10)
            {
                delayInSeconds = 10;
            }

            try
            {
                // To decouple our .exe call from the cmd.exe process, we also need to use "start" here.
                // Since we could have spaces in our .exe path, the path has to be surrounded by quotes.
                // Doing this properly, together with "start", its fist argument has to be empty quotes.
                // See here -> https://stackoverflow.com/questions/2937569/how-to-start-an-application-without-waiting-in-a-batch-file

                var psi = new ProcessStartInfo
                {
                    Arguments = $"/C ping 127.0.0.1 -n {delayInSeconds} && start \"\" \"{AppHelper.GetApplicationExecutableFilePath()}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                };

                var process = Process.Start(psi) ?? throw new InvalidOperationException("The 'Process.Start()' call returned null.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while restarting application (see log file for details).", e);
            }
        }

        public Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
                var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

                if (File.Exists(bakFilePath))
                {
                    File.Delete(bakFilePath);
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while removing .bak file of application update (see log file for details).", e);
            }

            return reliableFileOperations.WaitAsync(cancellationToken);
        }

        public Version GetInstalledVersion()
        {
            try
            {
                var installedExeFile = AppHelper.GetApplicationExecutableFilePath();
                var installedVersion = FileSystemHelper.GetExeFileVersion(installedExeFile);

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
            // Trust application settings and config validator (since this is business logic and not a helper) and therefore do no checks here

            return appSettings.Data.AppUpdateFolder;
        }

        private string PrepareForUpdateAndReturnNewExeFilePath(Version installedVersion)
        {
            try
            {
                var updateFolder = GetUpdateFolder();
                if (!Directory.Exists(updateFolder))
                {
                    throw new InvalidOperationException("Update folder not exists.");
                }

                var newExeFilePath = Path.Combine(updateFolder, appFileName);
                if (!File.Exists(newExeFilePath))
                {
                    throw new InvalidOperationException($"Update folder not contains {appFileName} file.");
                }

                var newExeVersion = FileSystemHelper.GetExeFileVersion(newExeFilePath);
                if (newExeVersion < installedVersion)
                {
                    throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
                }

                return newExeFilePath;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while application prepared for update (see log file for details).", e);
            }
        }
    }
}
