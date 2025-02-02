using System.Runtime.CompilerServices;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Modules;
using WOWCAM.Core.Parts.System;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper;

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
            var addonUrls = appSettings.AppSettings.AddonUrls;
            var targetFolder = appSettings.AppSettings.AddonTargetFolder;
            var workFolder = appSettings.AppSettings.WorkFolder;

            // Prepare folders

            var downloadFolder = appSettings.AppSettings.AddonDownloadFolder;
            if (Directory.Exists(downloadFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);
            }
            else
            {
                Directory.CreateDirectory(downloadFolder);
            }

            var webView = webViewProvider.GetWebView();
            webView.Profile.DefaultDownloadFolderPath = downloadFolder;

            var unzipFolder = appSettings.AppSettings.AddonUnzipFolder;
            if (Directory.Exists(unzipFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            }
            else
            {
                Directory.CreateDirectory(unzipFolder);
            }

            var smartUpdateFolder = appSettings.AppSettings.SmartUpdateFolder;
            if (!Directory.Exists(smartUpdateFolder))
            {
                Directory.CreateDirectory(smartUpdateFolder);
            }

            // Load SmartUpdate data

            try
            {
                await smartUpdateFeature.LoadAsync(cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while loading SmartUpdate data (see log file for details).");
                throw;
            }

            // Process addons

            uint updatedAddons;
            try
            {
                updatedAddons = await multiAddonProcessor.ProcessAddonsAsync(addonUrls, targetFolder, workFolder, progress, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while processing the addons (see log file for details).");
                throw;
            }

            // Save SmartUpdate data

            try
            {
                await smartUpdateFeature.SaveAsync(cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while saving SmartUpdate data (see log file for details).");
                throw;
            }

            // Move content and clean up

            await MoveContentAsync(unzipFolder, targetFolder, cancellationToken);
            await CleanUpAsync(downloadFolder, unzipFolder, cancellationToken);

            return updatedAddons;
        }

        private void HandleNonCancellationException(Exception orgException, string errorMessage, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(orgException, file, line);

            if (orgException is not TaskCanceledException && orgException is not OperationCanceledException)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        private async Task MoveContentAsync(string unzipFolder, string targetFolder, CancellationToken cancellationToken = default)
        {
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Clear target folder

            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
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
                throw new InvalidOperationException("An error occurred while moving the unzipped addons to target folder (see log file for details).");
            }
        }

        private async Task CleanUpAsync(string downloadFolder, string unzipFolder, CancellationToken cancellationToken = default)
        {
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Clean up temporary folders

            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while deleting the content of temporary folders (see log file for details).");
            }
        }
    }
}
