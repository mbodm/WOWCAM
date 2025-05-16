using Microsoft.Web.WebView2.Core;
using WOWCAM.Core.Parts.Logic.Addons;
using WOWCAM.Core.Parts.Logic.System;
using WOWCAM.Helper.Parts.System;
using WOWCAM.Logging;
using WOWCAM.WebView;

namespace WOWCAM.Core.Parts.Modules
{
    public sealed class DefaultAddonsModule(
        ILogger logger,
        IWebViewProvider webViewProvider,
        IWebViewWrapper webViewWrapper,
        ISettingsModule settingsModule,
        ISmartUpdateFeature smartUpdateFeature,
        IMultiAddonProcessor multiAddonProcessor,
        IReliableFileOperations reliableFileOperations) : IAddonsModule
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
        private readonly ISettingsModule settingsModule = settingsModule ?? throw new ArgumentNullException(nameof(settingsModule));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));
        private readonly IMultiAddonProcessor multiAddonProcessor = multiAddonProcessor ?? throw new ArgumentNullException(nameof(multiAddonProcessor));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));

        public bool HideDownloadDialog
        {
            get => webViewWrapper.HideDownloadDialog;
            set => webViewWrapper.HideDownloadDialog = value;
        }

        public void SetWebView(CoreWebView2 webView)
        {
            webViewProvider.SetWebView(webView);
        }

        public async Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = null, CancellationToken cancellationToken = default)
        {
            // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
            // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
            // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

            // Prepare folders

            var smartUpdateFolder = Path.Combine(settingsModule.SettingsData.WorkFolder, "SmartUpdate");
            var downloadFolder = Path.Combine(settingsModule.SettingsData.TempFolder, "Curse-Download");
            var unzipFolder = Path.Combine(settingsModule.SettingsData.TempFolder, "Curse-Unzip");
            var targetFolder = settingsModule.SettingsData.AddonTargetFolder;

            await PrepareFoldersAsync(downloadFolder, unzipFolder, targetFolder, cancellationToken);

            // Pepare WebView

            var webView = webViewProvider.GetWebView();
            webView.Profile.DefaultDownloadFolderPath = downloadFolder;

            // Prepare SmartUpdate

            await smartUpdateFeature.InitAsync(smartUpdateFolder, downloadFolder, cancellationToken);

            // Process addons

            await SmartUpdateLoadAsync(cancellationToken);

            uint countOfUpdatedAddons;
            try
            {
                countOfUpdatedAddons = await multiAddonProcessor.ProcessAddonsAsync(settingsModule.SettingsData.AddonUrls, downloadFolder, unzipFolder, progress, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                if (IsCancellationException(e)) throw;
                throw new InvalidOperationException("An error occurred while processing the addons (see log file for details).");
            }

            await SmartUpdateSaveAsync(cancellationToken);

            await MoveContentAsync(unzipFolder, targetFolder, cancellationToken);
            await CleanUpAsync(downloadFolder, unzipFolder, cancellationToken);

            return countOfUpdatedAddons;
        }

        private async Task PrepareFoldersAsync(string downloadFolder, string unzipFolder, string targetFolder, CancellationToken cancellationToken = default)
        {
            if (Directory.Exists(downloadFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Directory.CreateDirectory(downloadFolder);
            }

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
