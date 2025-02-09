using System.Collections.Concurrent;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultMultiAddonProcessor(ILogger logger, IAppSettings appSettings, ISingleAddonProcessor singleAddonProcessor) : IMultiAddonProcessor
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        private readonly ISingleAddonProcessor singleAddonProcessor = singleAddonProcessor ?? throw new ArgumentNullException(nameof(singleAddonProcessor));

        private readonly ConcurrentDictionary<string, uint> progressData = new();

        public async Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
        {
            // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
            // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
            // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

            logger.LogMethodEntry();

            var addonUrls = appSettings.Data.AddonUrls;

            // Prepare progress dictionary

            progressData.Clear();
            foreach (var addonUrl in addonUrls)
            {
                var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                progressData.TryAdd(addonName, 0);
            }

            // Concurrently do for every addon "fetch -> download -> unzip" (download part may be skipped/faked internally by SmartUpdate)

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
                            progressData[p.AddonName] = 200; // Just to make sure download is 100%
                            Interlocked.Increment(ref updatedAddonsCounter);
                            break;
                        case AddonState.DownloadFinishedBySmartUpdate:
                            progressData[p.AddonName] = 200;
                            break;
                        case AddonState.UnzipFinished:
                            progressData[p.AddonName] = 300;
                            break;
                    }

                    progress?.Report(CalcTotalPercent());
                });

                return singleAddonProcessor.ProcessAddonAsync(addonUrl, addonProgress, cancellationToken);
            });

            await Task.WhenAll(tasks);

            logger.LogMethodExit();

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
