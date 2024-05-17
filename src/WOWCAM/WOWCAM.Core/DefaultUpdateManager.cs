using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(
        ILogger logger, IAppHelper appHelper, IGitHubHelper gitHubHelper, IFileSystemHelper fileSystemHelper, IDownloadHelper downloadHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));

        public Version GetInstalledVersion()
        {
            try
            {
                var installedExeFile = Path.Combine(appHelper.GetApplicationExecutableFolder(), "WOWCAM.exe");
                var installedVersion = fileSystemHelper.GetExeFileVersion(installedExeFile);

                return installedVersion;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the installed WOWCAM version (see log file for details).", e);
            }
        }

        public async Task<bool> CheckForUpdates(CancellationToken cancellationToken = default)
        {
            var latestReleaseData = await GetLatestReleaseDataAsync(cancellationToken).ConfigureAwait(false);
            var installedVersion = GetInstalledVersion();

            return installedVersion < latestReleaseData.Version;
        }

        public async Task<bool> DownloadAndApplyUpdate(IProgress<ModelDownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default)
        {
            ModelGitHubLatestReleaseData? latestReleaseData;

            try
            {
                latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }

            var installedVersion = GetInstalledVersion();
            if (installedVersion < latestReleaseData.Version)
            {
                return false;
            }

            var downloadUrl = latestReleaseData.DownloadUrl;
            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException("Could not create URI from GitHub download URL.");
            }

            var fileName = uri.Segments.Last();
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            try
            {
                await downloadHelper.DownloadFileAsync(downloadUrl, filePath, downloadProgress, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while downloading latest WOWCAM release zip file from GitHub (see log file for details).", e);
            }

            return true;
        }

        private async Task<ModelGitHubLatestReleaseData> GetLatestReleaseDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }
        }
    }
}
