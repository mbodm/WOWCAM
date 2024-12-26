using WOWCAM.Helper;



// Todo: Use this comment at all appropriate locations.

// No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
// In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.



namespace WOWCAM.Core
{
    public sealed class DefaultAddonProcessing(ILogger logger, IWebViewWrapper webViewWrapper) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            var unzipFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Unzip");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            try
            {
                var tasks = addonUrls.Select(addonUrl => ProcessAddonAsync(addonUrl, downloadFolder, unzipFolder, cancellationToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Todo");
            }

            return;

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

        private async Task ProcessAddonAsync(string addonPageUrl, string downloadFolder, string unzipFolder, CancellationToken cancellationToken)
        {
            // Fetch JSON data

            var json = await webViewWrapper.NavigateToPageAndExecuteJavaScriptAsync(addonPageUrl, CurseHelper.FetchJsonScript, cancellationToken);
            var jsonModel = CurseHelper.SerializeAddonPageJson(json);
            var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);
            var zipFilePath = Path.Combine(downloadFolder, jsonModel.FileName);

            cancellationToken.ThrowIfCancellationRequested();

            // Download zip file

            try
            {
                await webViewWrapper.NavigateAndDownloadFileAsync(downloadUrl, null, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while downloading zip file (see log file for details).");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Validdate zip file

            try
            {
                if (!await ZipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("It seems the downloaded zip file is corrupted, cause zip file validation failed.");
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while validating zip file (see log file for details).");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Extract zip file

            try
            {
                await ZipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while extracting zip file (see log file for details).");
            }
        }
    }
}
