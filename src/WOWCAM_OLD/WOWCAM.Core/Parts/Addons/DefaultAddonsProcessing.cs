using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.System;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper.Parts;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultAddonsProcessing(
        ILogger logger,
        IAppSettings appSettings,
        IWebViewProvider webViewProvider,
        IMultiAddonProcessor multiAddonProcessor,
        ISmartUpdateFeature smartUpdateFeature,
        IReliableFileOperations reliableFileOperations) : IAddonsProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IMultiAddonProcessor multiAddonProcessor = multiAddonProcessor ?? throw new ArgumentNullException(nameof(multiAddonProcessor));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));

        public async Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = null, CancellationToken cancellationToken = default)
        {
            // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
            // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
            // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

            // Prepare folders

            await PrepareFoldersAsync(cancellationToken);

            var downloadFolder = appSettings.Data.AddonDownloadFolder;
            var unzipFolder = appSettings.Data.AddonUnzipFolder;
            var targetFolder = appSettings.Data.AddonTargetFolder;

            // Pepare WebView

            var webView = webViewProvider.GetWebView();
            webView.Profile.DefaultDownloadFolderPath = downloadFolder;

            // Load SmartUpdate data

            await SmartUpdateLoadAsync(cancellationToken);

            // Process addons

            uint updatedAddons;
            try
            {
                updatedAddons = await multiAddonProcessor.ProcessAddonsAsync(progress, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while processing the addons (see log file for details).");
            }

            // Save SmartUpdate data

            await SmartUpdateSaveAsync(cancellationToken);

            // Move content and clean up

            await MoveContentAsync(unzipFolder, targetFolder, cancellationToken);
            await CleanUpAsync(downloadFolder, unzipFolder, cancellationToken);

            return updatedAddons;
        }

        private async Task PrepareFoldersAsync(CancellationToken cancellationToken = default)
        {
            var downloadFolder = appSettings.Data.AddonDownloadFolder;
            if (Directory.Exists(downloadFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Directory.CreateDirectory(downloadFolder);
            }

            var unzipFolder = appSettings.Data.AddonUnzipFolder;
            if (Directory.Exists(unzipFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Directory.CreateDirectory(unzipFolder);
            }

            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Just to be sure (even when target folder was already handled by config validation)

            var targetFolder = appSettings.Data.AddonTargetFolder;
            if (!Directory.Exists(targetFolder))
            {
                throw new InvalidOperationException("Configured target folder not exists.");
            }
        }

        private async Task SmartUpdateLoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await smartUpdateFeature.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while loading SmartUpdate data (see log file for details).");
            }
        }

        private async Task SmartUpdateSaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await smartUpdateFeature.SaveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while saving SmartUpdate data (see log file for details).");
            }
        }

        private async Task MoveContentAsync(string unzipFolder, string targetFolder, CancellationToken cancellationToken = default)
        {
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Clear the target folder

            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while deleting the content of target folder (see log file for details).");
            }

            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Move to target folder

            try
            {
                await FileSystemHelper.MoveFolderContentAsync(unzipFolder, targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while moving the unzipped addons to target folder (see log file for details).");
            }
        }

        private async Task CleanUpAsync(string downloadFolder, string unzipFolder, CancellationToken cancellationToken = default)
        {
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Clean up temporary folders

            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while deleting the content of temporary folders (see log file for details).");
            }
        }

        private static bool IsCancellationException(Exception e) => e is TaskCanceledException || e is OperationCanceledException;
    }
}
