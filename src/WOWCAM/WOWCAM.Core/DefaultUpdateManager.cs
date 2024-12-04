using System.Diagnostics;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(ILogger logger, IConfig config, HttpClient httpClient) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        private readonly string appName = AppHelper.GetApplicationName();
        private readonly string appFileName = AppHelper.GetApplicationExecutableFileName();

        public async Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();

            try
            {
                var latestReleaseData = await GitHubHelper.GetLatestReleaseData("mbodm", "wowcam", httpClient, cancellationToken).ConfigureAwait(false);
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
            IProgress<DownloadHelperProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
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
                    await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
                }

                await DownloadHelper.DownloadFileAsync(httpClient, updateData.UpdateDownloadUrl, releaseZipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);

                if (!File.Exists(releaseZipFilePath))
                    throw new InvalidOperationException("Downloaded latest release, but update folder not contains zip file.");

                await ZipFileHelper.ExtractZipFileAsync(releaseZipFilePath, updateFolder, cancellationToken).ConfigureAwait(false);

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

            string newExeFilePath;

            try
            {
                var updateFolder = GetUpdateFolder();
                if (!Directory.Exists(updateFolder))
                    throw new InvalidOperationException("Update folder not exists.");

                newExeFilePath = Path.Combine(updateFolder, appFileName);
                if (!File.Exists(newExeFilePath))
                    throw new InvalidOperationException($"Update folder not contains {appFileName} file.");

                var newExeVersion = FileSystemHelper.GetExeFileVersion(newExeFilePath);
                if (newExeVersion < installedVersion)
                    throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while application prepared for update (see log file for details).", e);
            }

            // Overwrite application executable

            try
            {
                var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
                var updFilePath = newExeFilePath;
                var bakFilePath = Path.ChangeExtension(AppHelper.GetApplicationExecutableFilePath(), ".bak");

                File.Move(exeFilePath, bakFilePath, true);
                File.Copy(updFilePath, exeFilePath, true);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not apply update (see log file for details).", e);
            }
        }

        public void RestartApplication()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    Arguments = $"/C ping 127.0.0.1 -n 2 && \"{AppHelper.GetApplicationExecutableFilePath()}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });

                if (process == null)
                    throw new InvalidOperationException("The 'Process.Start()' call returned null.");
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while restarting application (see log file for details).", e);
            }
        }

        private Version GetInstalledVersion()
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
            // Trust application and config validator (since this is business logic and not a helper) and therefore do no temp folder check here

            return Path.Combine(config.TempFolder, "MBODM-WOWCAM-Update");
        }
    }
}
