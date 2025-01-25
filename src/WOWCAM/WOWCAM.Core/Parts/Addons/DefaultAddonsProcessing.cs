using System.Collections.Concurrent;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons
{
    // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
    // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
    // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

    public sealed class DefaultAddonsProcessing(
        ILogger logger, IAddonProcessing addonProcessing, IWebViewProvider webViewProvider, IWebViewWrapper webViewWrapper, ISmartUpdateFeature smartUpdateFeature) : IAddonsProcessing
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAddonProcessing addonProcessing = addonProcessing ?? throw new ArgumentNullException(nameof(addonProcessing));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));

        private readonly ConcurrentDictionary<string, uint> progressData = new();

        public async Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string targetFolder, string workFolder, bool showDownloadDialog = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(addonUrls);

            if (!addonUrls.Any())
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException($"'{nameof(targetFolder)}' cannot be null or whitespace.", nameof(targetFolder));
            }

            if (string.IsNullOrWhiteSpace(workFolder))
            {
                throw new ArgumentException($"'{nameof(workFolder)}' cannot be null or whitespace.", nameof(workFolder));
            }

            webViewWrapper.HideDownloadDialog = !showDownloadDialog;

            // Prepare download folder

            var downloadFolder = Path.Combine(workFolder, "MBODM-WOWCAM-Addons-Download");
            if (Directory.Exists(downloadFolder)) await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);
            else Directory.CreateDirectory(downloadFolder);

            var webView = webViewProvider.GetWebView();
            webView.Profile.DefaultDownloadFolderPath = downloadFolder;

            // Prepare unzip folder

            var unzipFolder = Path.Combine(workFolder, "MBODM-WOWCAM-Addons-Unzip");
            if (Directory.Exists(unzipFolder)) await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            else Directory.CreateDirectory(unzipFolder);
            
            // Prepare progress dictionary

            progressData.Clear();
            foreach (var addonUrl in addonUrls)
            {
                var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progressData.TryAdd(addonName, 0);
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

            // Concurrently do for every addon "fetch -> download -> unzip" (or maybe skip last 2 parts if SmartUpdate is active)

            uint updatedAddonsCounter = 0;
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
                                Interlocked.Increment(ref updatedAddonsCounter);
                                break;
                            case AddonState.NoNeedToDownload:
                                progressData[p.AddonName] = 300;
                                break;
                        }

                        progress?.Report(CalcTotalPercent());
                    });

                    return addonProcessing.ProcessAddonAsync(addonUrl, downloadFolder, unzipFolder, addonProgress, cancellationToken);
                });

                await Task.WhenAll(tasks);
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

            // Move and clean up

            await MoveAsync(unzipFolder, targetFolder, cancellationToken);
            await CleanUpAsync(downloadFolder, unzipFolder, cancellationToken);

            return updatedAddonsCounter;
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

        private async Task MoveAsync(string unzipFolder, string targetFolder, CancellationToken cancellationToken = default)
        {
            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(250, cancellationToken);

            // Clear target folder
            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while deleting the content of target folder (see log file for details).");
                throw;
            }

            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(250, cancellationToken);

            // Move to target folder
            try
            {
                await FileSystemHelper.MoveFolderContentAsync(unzipFolder, targetFolder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while moving the unzipped addons to target folder (see log file for details).");
                throw;
            }
        }

        private async Task CleanUpAsync(string downloadFolder, string unzipFolder, CancellationToken cancellationToken = default)
        {
            // All operations are done for sure here, but the hardware buffers (or virus scan, or whatever) has not finished yet.
            // Therefore give em time to finish their business. There is no other way, since this is not under the app's control.
            await Task.Delay(250, cancellationToken);

            // Clean up temporary folders
            try
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken);
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken);
            }
            catch (Exception e)
            {
                HandleNonCancellationException(e, "An error occurred while deleting the content of temporary folders (see log file for details).");
                throw;
            }
        }
    }
}
