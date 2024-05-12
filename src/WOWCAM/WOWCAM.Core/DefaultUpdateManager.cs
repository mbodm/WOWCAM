using System.Diagnostics;
using System.Text.Json;
using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultUpdateManager(ILogger logger, IAppHelper appHelper, IGitHubHelper gitHubHelper) : IUpdateManager
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppHelper appHelper = appHelper ?? throw new ArgumentNullException(nameof(appHelper));
        private readonly IGitHubHelper gitHubHelper = gitHubHelper ?? throw new ArgumentNullException(nameof(gitHubHelper));

        public async Task<bool> CheckForUpdates(CancellationToken cancellationToken = default)
        {
            var actualVersion = GetActualVersion();

            try
            {
                var latestReleaseData = await gitHubHelper.GetLatestReleaseData(cancellationToken).ConfigureAwait(false);

                return latestReleaseData.Version > actualVersion;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine the latest WOWCAM version on GitHub (see log file for details).", e);
            }

            /*
            try
            {
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Error while downloading latest WOWCAM release zip file from GitHub (see log file for details).", e);
            }
            */
        }

        public Task DownloadAndApplyUpdate(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private Version GetActualVersion()
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
                throw new InvalidOperationException("Could not determine the current WOWCAM version (see log file for details).", e);
            }
        }
    }
}
