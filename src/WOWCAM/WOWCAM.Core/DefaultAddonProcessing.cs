using Microsoft.Web.WebView2.Core;
using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public sealed class DefaultAddonProcessing(
        ILogger logger, ICurseHelper curseHelper, IWebViewHelper webViewHelper, IDownloadHelper downloadHelper, IZipFileHelper zipFileHelper, IFileSystemHelper fileSystemHelper) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICurseHelper curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
        private readonly IWebViewHelper webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));

        public async Task ProcessAddonsAsync(
            CoreWebView2 coreWebView, IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
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

            var addonDownloadUrlDataList = new List<ModelAddonDownloadUrlData>();

            // This needs to happen sequential, cause of WebView2 behavior!
            // Therefore do not use concurrency, like Task.WhenAll(), here!

            foreach (var addonUrl in addonUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var slugName = curseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.StartingFetch, slugName));

                var addonDownloadUrlData = await webViewHelper.GetAddonDownloadUrlDataAsync(coreWebView, addonUrl);
                addonDownloadUrlDataList.Add(addonDownloadUrlData);

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

            var tasks = addonDownloadUrlDataList.Select(addonDownloadUrlData => ProcessAddonAsync(addonDownloadUrlData, downloadFolder, unzipFolder, progress, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.

            await Task.Delay(200, cancellationToken);

            // Clear target folder

            try
            {
                await fileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while deleting the content of target folder (see log file for details).");
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.

            await Task.Delay(200, cancellationToken);

            // Move to target folder

            try
            {
                await fileSystemHelper.MoveFolderContentAsync(unzipFolder, targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while moving the unzipped addons to target folder (see log file for details).");
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.

            await Task.Delay(200, cancellationToken);

            // Clean up temp folder

            try
            {
                await fileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
                await fileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while deleting the content of temp folder (see log file for details).");
            }
        }

        private async Task ProcessAddonAsync(ModelAddonDownloadUrlData addonDownloadUrlData, string downloadFolder, string unzipFolder,
            IProgress<ModelAddonProcessingProgress>? progress, CancellationToken cancellationToken)
        {
            var downloadUrl = addonDownloadUrlData.DownloadUrl;
            var zipFilePath = Path.Combine(downloadFolder, addonDownloadUrlData.FileName);

            // Download zip file

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ModelAddonProcessingProgress(EnumAddonProcessingState.StartingDownload, addonDownloadUrlData.FileName));

            try
            {
                await downloadHelper.DownloadFileAsync(downloadUrl, zipFilePath, null, cancellationToken).ConfigureAwait(false);
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

            if (!await zipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken).ConfigureAwait(false))
            {
                var message = "Downloaded zip file is corrupted (see log file for details).";
                logger.Log(message);
                throw new InvalidOperationException(message);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await zipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
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
