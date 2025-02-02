using System.Collections.Concurrent;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons
{
    // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
    // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
    // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

    public sealed class DefaultMultiAddonProcessor(
        ILogger logger, ISingleAddonProcessor singleAddonProcessor, IWebViewProvider webViewProvider, ISmartUpdateFeature smartUpdateFeature) : IMultiAddonProcessor
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ISingleAddonProcessor singleAddonProcessor = singleAddonProcessor ?? throw new ArgumentNullException(nameof(singleAddonProcessor));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));

        private readonly ConcurrentDictionary<string, uint> progressData = new();

        public async Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string targetFolder, string workFolder,
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

            // Prepare progress dictionary

            progressData.Clear();
            foreach (var addonUrl in addonUrls)
            {
                var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progressData.TryAdd(addonName, 0);
            }

            // Concurrently do for every addon "fetch -> download -> unzip" (download part may be skipped/faked by SmartUpdate)

            uint updatedAddonsCounter = 0;

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
                        case AddonState.DownloadFinishedCauseSmartUpdate:
                            progressData[p.AddonName] = 200;
                            break;
                        case AddonState.UnzipFinished:
                            progressData[p.AddonName] = 300;
                            Interlocked.Increment(ref updatedAddonsCounter);
                            break;
                    }

                    progress?.Report(CalcTotalPercent());
                });

                return singleAddonProcessor.ProcessAddonAsync(addonUrl, downloadFolder, unzipFolder, addonProgress, cancellationToken);
            });

            await Task.WhenAll(tasks);

            return updatedAddonsCounter;
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
    }
}
