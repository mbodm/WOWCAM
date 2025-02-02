using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Update;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Modules
{
    public sealed class AppUpdateModule : IAppUpdateModule
    {
        private readonly ILogger logger;
        private readonly IUpdateManager updateManager;

        public AppUpdateModule(ILogger logger, IUpdateManager updateManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
        }

        public async Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await updateManager.CheckForUpdateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);

                var appName = AppHelper.GetApplicationName();
                throw new InvalidOperationException($"Could not determine the latest {appName} version (see log file for details).", e);
            }
        }

        public Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ApplyUpdateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void RestartApplication(uint delayInSeconds)
        {
            throw new NotImplementedException();
        }

        public Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
