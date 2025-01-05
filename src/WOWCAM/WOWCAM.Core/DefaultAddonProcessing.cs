using System.Collections.Concurrent;
using WOWCAM.Helper;

namespace WOWCAM.Core
{
    // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
    // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
    // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

    public sealed class DefaultAddonProcessing(
        ILogger logger, IWebViewProvider webViewProvider, IWebViewWrapper webViewWrapper, ISmartUpdateFeature smartUpdateFeature) : IAddonProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));

        private readonly ConcurrentDictionary<string, uint> progressData = new();

        private enum AddonState { FetchFinished, DownloadProgress, DownloadFinished, UnzipFinished, NoNeedToUpdateBySUF }
        private sealed record AddonProgress(AddonState AddonState, string AddonName, byte DownloadPercent);

        public async Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder, bool smartUpdate = false, bool showDownloadDialog = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(addonUrls);

            if (!addonUrls.Any())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tempFolder))
            {
                throw new ArgumentException($"'{nameof(tempFolder)}' cannot be null or whitespace.", nameof(tempFolder));
            }

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException($"'{nameof(targetFolder)}' cannot be null or whitespace.", nameof(targetFolder));
            }

            webViewWrapper.HideDownloadDialog = !showDownloadDialog;

            // Prepare folders

            var downloadFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Download");
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

            var unzipFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Unzip");
            if (Directory.Exists(unzipFolder))
            {
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            }
            else
            {
                Directory.CreateDirectory(unzipFolder);
            }

            // Prepare progress dictionary

            progressData.Clear();
            foreach (var addonUrl in addonUrls)
            {
                var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progressData.TryAdd(addonName, 0);
            }

            // Handle SmartUpdate mode

            if (smartUpdate)
            {
                await smartUpdateFeature.CreateStorageIfNotExistsAsync(cancellationToken);
            }
            else
            {
                await smartUpdateFeature.RemoveStorageIfExistsAsync(cancellationToken);
            }

            // Concurrenly do for every addon "fetch -> download -> unzip"

            try
            {
                var tasks = addonUrls.Select(addonUrl =>
                {
                    var addonProgress = new Progress<AddonProgress>(p =>
                    {
                        switch (p.AddonState)
                        {
                            case AddonState.FetchFinished:
                                progressData[p.AddonName] = 100;
                                break;
                            case AddonState.DownloadProgress:
                                progressData[p.AddonName] = 100u + p.DownloadPercent;
                                break;
                            case AddonState.DownloadFinished:
                                // Just to make sure download is 100%
                                progressData[p.AddonName] = 200;
                                break;
                            case AddonState.UnzipFinished:
                                progressData[p.AddonName] = 300;
                                break;
                            case AddonState.NoNeedToUpdateBySUF:
                                progressData[p.AddonName] = 300;
                                break;
                        }

                        progress?.Report(CalcTotalPercent());
                    });

                    return ProcessAddonAsync(addonUrl, downloadFolder, unzipFolder, smartUpdate, addonProgress, cancellationToken);
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while processing the addons (see log file for details).");
                throw;
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken);

            // Clear target folder
            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while deleting the content of target folder (see log file for details).");
                throw;
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken);

            // Move to target folder
            try
            {
                await FileSystemHelper.MoveFolderContentAsync(unzipFolder, targetFolder, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while moving the unzipped addons to target folder (see log file for details).");
                throw;
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(200, cancellationToken);

            // Clean up temp folder
            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while deleting the content of temp folder (see log file for details).");
                throw;
            }
        }

        private async Task ProcessAddonAsync(string addonPageUrl, string downloadFolder, string unzipFolder, bool smartUpdate,
            IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonPageUrl);

            // Fetch JSON data
            cancellationToken.ThrowIfCancellationRequested();
            CurseAddonPageJson jsonModel;
            try
            {
                var json = await webViewWrapper.NavigateToPageAndExecuteJavaScriptAsync(addonPageUrl, CurseHelper.FetchJsonScript, cancellationToken);
                jsonModel = CurseHelper.SerializeAddonPageJson(json);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while fetching JSON data from addon page (see log file for details).");
                throw;
            }
            progress?.Report(new AddonProgress(AddonState.FetchFinished, addonName, 0));

            // Build download URL
            var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);

            // Handle SmartUpdate mode
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (smartUpdate)
                {
                    var exists = await smartUpdateFeature.ExactEntryExistsAsync(addonName, downloadUrl, cancellationToken);
                    if (exists)
                    {
                        progress?.Report(new AddonProgress(AddonState.NoNeedToUpdateBySUF, addonName, 100));
                        return;
                    }

                    await smartUpdateFeature.AddOrUpdateEntryAsync(addonName, downloadUrl, cancellationToken);
                }
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while using SmartUpdate feature (see log file for details).");
                throw;
            }

            // Download zip file
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var downloadProgress = new Progress<WebViewWrapperDownloadProgress>(p =>
                {
                    var percent = CalcDownloadPercent(p.ReceivedBytes, p.TotalBytes);
                    progress?.Report(new AddonProgress(AddonState.DownloadProgress, addonName, percent));
                });

                await webViewWrapper.NavigateAndDownloadFileAsync(downloadUrl, downloadProgress, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while downloading zip file (see log file for details).");
                throw;
            }
            progress?.Report(new AddonProgress(AddonState.DownloadFinished, addonName, 100));

            // Extract zip file
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var zipFilePath = Path.Combine(downloadFolder, jsonModel.FileName);

                if (!await ZipFileHelper.ValidateZipFileAsync(zipFilePath, cancellationToken))
                {
                    throw new InvalidOperationException("It seems the downloaded zip file is corrupted, cause zip file validation failed.");
                }

                await ZipFileHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while extracting zip file (see log file for details).");
                throw;
            }
            progress?.Report(new AddonProgress(AddonState.UnzipFinished, addonName, 100));
        }

        private void HandleNonCancellationException(Exception orgException, string bunchMessage)
        {
            logger.Log(orgException);

            if (orgException is not TaskCanceledException && orgException is not OperationCanceledException)
            {
                throw new InvalidOperationException(bunchMessage);
            }
        }

        private byte CalcTotalPercent()
        {
            // Doing casts inside try/catch block (just to be sure)

            try
            {
                var sumOfAllAddons = (ulong)progressData.Sum(kvp => kvp.Value);
                var hundredPercent = (ulong)progressData.Count * 300;

                var exact = (double)sumOfAllAddons / hundredPercent;
                var exactPercent = exact * 100;
                var roundedPercent = (byte)Math.Round(exactPercent);
                var cappedPercent = roundedPercent > 100 ? (byte)100 : roundedPercent; // Cap it (just to be sure)

                return cappedPercent;
            }
            catch
            {
                return 0;
            }
        }

        private static byte CalcDownloadPercent(uint bytesReceived, uint bytesTotal)
        {
            // Doing casts inside try/catch block (just to be sure)

            try
            {
                var exact = (double)bytesReceived / bytesTotal;
                var exactPercent = exact * 100;
                var roundedPercent = (byte)Math.Round(exactPercent);
                var cappedPercent = roundedPercent > 100 ? (byte)100 : roundedPercent; // Cap it (just to be sure)

                return cappedPercent;
            }
            catch
            {
                return 0;
            }
        }
    }
}
