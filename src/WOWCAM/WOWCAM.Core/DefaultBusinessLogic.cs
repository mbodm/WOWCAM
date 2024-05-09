using System.ComponentModel.DataAnnotations;
using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public sealed class DefaultBusinessLogic(
        ILogger logger, IWebViewHelper webViewHelper, IDownloadHelper downloadHelper, IZipFileHelper zipFileHelper, IFileSystemHelper fileSystemHelper) : IBusinessLogic
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

                progress?.Report(true);
            }

            var downloadFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Zip-Downloads");

            var tasks = downloadUrlDataList.Select(downloadUrlData =>
            {
                var downloadUrl = downloadUrlData.DownloadUrl;
                var filePath = Path.Combine(downloadFolder, downloadUrlData.FileName);
                var task1 = downloadHelper.DownloadAddonAsync(downloadUrl, filePath, cancellationToken);
                var task2 = task1.ContinueWith(t => progress?.Report(true));

                return task2;
            });

            await Task.WhenAll(tasks);

            var unzipSourceFolder = downloadFolder;
            var unzipDestFolder = targetFolder;

            var zipFiles = GetZipFiles(unzipSourceFolder);
            var validateTasks = zipFiles.Select(zipFile => zipFileHelper.ValidateZipFileAsync(zipFile, cancellationToken));
            bool[] validateResults;

            try
            {
                validateResults = await Task.WhenAll(validateTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("An error occurred while validating the zip files in Source-Folder (see log file for details).");
            }

            if (validateResults.Contains(false))
            {
                logger.Log("Open or read failed for one or more zip files.");
                var additionalHint = "No data in Destination-Folder has changed!";
                throw new InvalidOperationException($"Source-Folder contains corrupted zip files (see log file for details). {additionalHint}");
            }

            try
            {
                await fileSystemHelper.DeleteFolderContentAsync(unzipDestFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("An error occurred while deleting the content of Destination-Folder (see log file for details).");
            }

            var unzipTasks = zipFiles.Select(zipFile => Task.Run(() =>
            {
                // No need for ThrowIfCancellationRequested() here, since Task.Run() cancels on its own if the task
                // has not already started. Also this workload is "atomic" (if a file was unzipped, it is a progress).

                try
                {
                    // No need for an async TAP call here, since this runs already inside a Task.Run() method.
                    // Therefore the TAP call is done sync here, to not create some unnecessary awaiter effort.

                    zipFileHelper.ExtractZipFileAsync(zipFile, unzipDestFolder, cancellationToken).Wait();
                }
                catch (Exception e)
                {
                    logger.Log(e);
                    throw;
                }

                progress?.Report(true);
            },
            cancellationToken));

            try
            {
                await Task.WhenAll(unzipTasks);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("An error occurred while extracting the zip files (see log file for details).");
            }
        }

        private IEnumerable<string> GetZipFiles(string sourceFolder)
        {
            IEnumerable<string> zipFiles;

            try
            {
                zipFiles = fileSystemHelper.GetAllZipFilesInFolder(sourceFolder);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not determine zip files in Source-Folder (see log file for details).");
            }

            if (!zipFiles.Any())
            {
                throw new ValidationException("Source-Folder not contains any zip files.");
            }

            return zipFiles;
        }
    }
}
