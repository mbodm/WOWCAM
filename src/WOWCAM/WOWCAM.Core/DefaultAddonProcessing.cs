using Microsoft.Web.WebView2.Core;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed class DefaultAddonProcessing(ILogger logger, IWebViewWrapper webViewWrapper, HttpClient httpClient) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task ProcessAddonsAsync(CoreWebView2 coreWebView, IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<ModelAddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coreWebView);
            ArgumentNullException.ThrowIfNull(addonUrls);

            if (string.IsNullOrWhiteSpace(tempFolder))
            {
                throw new ArgumentException($"'{nameof(tempFolder)}' cannot be null or whitespace.", nameof(tempFolder));
            }

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException($"'{nameof(targetFolder)}' cannot be null or whitespace.", nameof(targetFolder));
            }

            // Fetch JSON data

            var addonDownloadDataList = new List<ModelAddonDownloadData>();

            // This needs to happen sequential, cause of WebView2 behavior!
            // Therefore do not use concurrency, like Task.WhenAll(), here!

            foreach (var addonUrl in addonUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var slugName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.StartingFetch, slugName));

                try
                {
                    // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
                    // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.

                    var addonDownloadUrlData = await webViewWrapper.GetAddonDownloadUrlDataAsync(coreWebView, addonUrl);
                    addonDownloadDataList.Add(addonDownloadUrlData);
                }
                catch (Exception e)
                {
                    logger.Log(e);
                    throw new InvalidOperationException("An error occurred while fetching JSON from the addon pages (see log file for details).");
                }

                progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.FinishedFetch, slugName));
            }

            var downloadFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Download");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            var unzipFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Unzip");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            // Download and Unzip

            try
            {
                var tasks = addonDownloadDataList.Select(addonDownloadUrlData => ProcessAddonAsync(addonDownloadUrlData, downloadFolder, unzipFolder, progress, cancellationToken));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while downloading and unzipping the addons (see log file for details).");
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

        private async Task ProcessAddonAsync(ModelAddonDownloadData addonDownloadUrlData, string downloadFolder, string unzipFolder,
            IProgress<ModelAddonProcessingProgress>? progress, CancellationToken cancellationToken)
        {
            var downloadUrl = addonDownloadUrlData.DownloadUrl;
            var zipFilePath = Path.Combine(downloadFolder, addonDownloadUrlData.FileName);

            // Download zip file

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.StartingDownload, addonDownloadUrlData.FileName));

            try
            {
                await DownloadHelper.DownloadFileAsync(httpClient, downloadUrl, zipFilePath, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while downloading zip file (see log file for details).");
            }

            progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.FinishedDownload, addonDownloadUrlData.FileName));

            // Validdate & Extract zip file

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.StartingUnzip, addonDownloadUrlData.FileName));

            if (!await ZipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken).ConfigureAwait(false))
            {
                var message = "Downloaded zip file is corrupted (see log file for details).";
                logger.Log(message);
                throw new InvalidOperationException(message);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ZipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while extracting zip file (see log file for details).");
            }

            progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.FinishedUnzip, addonDownloadUrlData.FileName));
        }
    }
}
