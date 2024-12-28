using WOWCAM.Helper;

// Todo: Use this comment at all appropriate locations.

// No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
// In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.

namespace WOWCAM.Core
{
    public sealed class DefaultAddonProcessing(ILogger logger, IWebViewProvider webViewProvider, IWebViewWrapper webViewWrapper) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));

        public async Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<AddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(addonUrls);

            if (string.IsNullOrWhiteSpace(tempFolder))
            {
                throw new ArgumentException($"'{nameof(tempFolder)}' cannot be null or whitespace.", nameof(tempFolder));
            }

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException($"'{nameof(targetFolder)}' cannot be null or whitespace.", nameof(targetFolder));
            }

            var downloadFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Download");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);

            var webView = webViewProvider.GetWebView();
            webView.Profile.DefaultDownloadFolderPath = downloadFolder;

            var unzipFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Unzip");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);

            try
            {
                var tasks = addonUrls.Select(addonUrl => ProcessAddonAsync(addonUrl, downloadFolder, unzipFolder, progress, cancellationToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException($"Todo: {e.Message}");
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

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

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

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

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

            // Clean up temp folder

            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while deleting the content of temp folder (see log file for details).");
            }
        }

        private async Task ProcessAddonAsync(string addonPageUrl, string downloadFolder, string unzipFolder,
            IProgress<AddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonPageUrl);

            // Fetch JSON data

            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.StartingFetch, addonName, 0));

            CurseAddonPageJson jsonModel;
            try
            {
                var json = await webViewWrapper.NavigateToPageAndExecuteJavaScriptAsync(addonPageUrl, CurseHelper.FetchJsonScript, cancellationToken);
                jsonModel = CurseHelper.SerializeAddonPageJson(json);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while fetching JSON data from addon page (see log file for details).");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.FinishedFetch, addonName, 0));

            // Download zip file

            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.StartingDownload, addonName, 0));

            try
            {
                var downloadProgress = new Progress<WebViewWrapperDownloadProgress>(p =>
                {
                    var percent = CalcDownloadPercent(p.ReceivedBytes, p.TotalBytes);
                    progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.Downloading, addonName, percent));
                });
                
                var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);
                await webViewWrapper.NavigateAndDownloadFileAsync(downloadUrl, downloadProgress, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while downloading zip file (see log file for details).");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.FinishedDownload, addonName, 100));

            // Validate & Extract zip file

            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.StartingUnzip, addonName, 100));

            try
            {
                var zipFilePath = Path.Combine(downloadFolder, jsonModel.FileName);

                if (!await ZipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("It seems the downloaded zip file is corrupted, cause zip file validation failed.");
                }

                await ZipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while extracting zip file (see log file for details).");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new AddonProcessingProgress(AddonProcessingProgressState.FinishedUnzip, addonName, 100));
        }

        private static byte CalcDownloadPercent(uint bytesReceived, uint bytesTotal)
        {
            // Doing casts inside try/catch block, just to be sure.

            try
            {
                double exact = (double)bytesReceived / bytesTotal * 100;
                byte rounded = (byte)Math.Round(exact);
                byte percent = rounded > 100 ? (byte)100 : rounded; // Cap it (just to be sure)

                return percent;
            }
            catch
            {
                return 0;
            }
        }
    }
}
