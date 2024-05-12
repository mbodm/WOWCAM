using System.Diagnostics;
using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(ILogger logger, IAppHelper appHelper, IGitHubHelper gitHubHelper, IDownloadHelper downloadHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));

        public Version GetInstalledVersion()
        {
            try
            {
                var exeFolder = appHelper.GetApplicationExecutableFolder();
                var exePath = Path.Combine(exeFolder, "WOWCAM.exe");
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(exePath);
                var productVersion = fileVersionInfo.ProductVersion ?? throw new InvalidOperationException("Could not determine product version of exe file.");

                return new Version(productVersion);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the installed WOWCAM version (see log file for details).", e);
            }
        }

        public async Task<bool> CheckForUpdates(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();

            try
            {
                var latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);

                return installedVersion < latestReleaseData.Version;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }
        }

        public async Task<bool> DownloadAndApplyUpdate(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();

            ModelGitHubLatestReleaseData? latestReleaseData;

            try
            {
                latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);

                if (installedVersion < latestReleaseData.Version)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }

            var downloadUrl = latestReleaseData.DownloadUrl;

            // Todo: Add progress.

            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException("Todo");
            }

            var fileName = uri.Segments.Last();
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            try
            {
                await downloadHelper.DownloadFileAsync(downloadUrl, filePath, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while downloading latest WOWCAM release zip file from GitHub (see log file for details).", e);
            }

            return true;
        }
    }
}
