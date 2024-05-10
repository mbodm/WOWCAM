using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public sealed class DefaultAddonProcessing(
        ILogger logger, IWebViewHelper webViewHelper, IDownloadHelper downloadHelper, IZipFileHelper zipFileHelper, IFileSystemHelper fileSystemHelper) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewHelper webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));
        private readonly IDownloadHelper downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));

        public async Task ProcessAddonsAsync(
            CoreWebView2 coreWebView, IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<bool>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coreWebView);

            if (string.IsNullOrWhiteSpace(tempFolder))
            {
                throw new ArgumentException($"'{nameof(tempFolder)}' cannot be null or whitespace.", nameof(tempFolder));
            }

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException($"'{nameof(targetFolder)}' cannot be null or whitespace.", nameof(targetFolder));
            }

            var downloadUrlDataList = new List<ModelDownloadUrlData>();

            // This needs to happen sequential, cause of WebView2 behavior!
            // Therefore do not use concurrency, like Task.WhenAll(), here!

            foreach (var addonUrl in addonUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var downloadUrlData = await webViewHelper.GetDownloadUrlDataAsync(coreWebView, addonUrl);

                downloadUrlDataList.Add(downloadUrlData);

                progress?.Report(true);
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

            var tasks = downloadUrlDataList.Select(downloadUrlData => ProcessAddonAsync(downloadUrlData, downloadFolder, unzipFolder, progress, cancellationToken));
            await Task.WhenAll(tasks);

            try
            {
                await fileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("An error occurred while deleting the content of target folder (see log file for details).");
            }

            // Todo: Move all content from download folder to target folder

            var dirInfo = new DirectoryInfo(unzipFolder);
            var dirNames = dirInfo.GetDirectories().Select(dir => dir.Name);

            dirNames.ToList().ForEach(dirName =>
            {
                var sourceDir = Path.Combine(unzipFolder, dirName);
                var destDir = Path.Combine(targetFolder, dirName);
                
                Directory.Move(sourceDir, destDir);
            });
        }

        private async Task ProcessAddonAsync(ModelDownloadUrlData downloadUrlData, string downloadFolder, string unzipFolder,
            IProgress<bool>? progress, CancellationToken cancellationToken)
        {
            var downloadUrl = downloadUrlData.DownloadUrl;
            var zipFilePath = Path.Combine(downloadFolder, downloadUrlData.FileName);

            // Download zip file

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await downloadHelper.DownloadAddonAsync(downloadUrl, zipFilePath, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while downloading zip file (see log file for details).");
            }

            progress?.Report(true);

            // Validate zip file

            cancellationToken.ThrowIfCancellationRequested();

            if (!await zipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken))
            {
                var message = "Detected corrupt zip file (see log file for details).";
                logger.Log(message);
                throw new InvalidOperationException(message);
            }

            progress?.Report(true);

            // Extract zip file

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await zipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while extracting zip file (see log file for details).");
            }

            progress?.Report(true);
        }
    }
}
