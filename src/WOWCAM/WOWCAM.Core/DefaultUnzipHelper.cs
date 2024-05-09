using System.ComponentModel.DataAnnotations;

namespace WOWCAM.Core
{
    public sealed class DefaultUnzipHelper(ILogger logger, IFileSystemHelper fileSystemHelper, IZipFileHelper zipFileHelper) : IUnzipHelper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IFileSystemHelper fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        private readonly IZipFileHelper zipFileHelper = zipFileHelper ?? throw new ArgumentNullException(nameof(zipFileHelper));

        public async Task ExtractZipFilesAsync(
            string sourceFolder, string destFolder, IProgress<ModelUnzipHelperProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            var zipFiles = GetZipFiles(sourceFolder);
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
                await fileSystemHelper.DeleteFolderContentAsync(destFolder, cancellationToken).ConfigureAwait(false);
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

                    zipFileHelper.ExtractZipFileAsync(zipFile, destFolder, cancellationToken).Wait();
                }
                catch (Exception e)
                {
                    logger.Log(e);
                    throw;
                }

                progress?.Report(new ModelUnzipHelperProgress(zipFile));
            },
            cancellationToken));

            try
            {
                await Task.WhenAll(unzipTasks).ConfigureAwait(false);
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
