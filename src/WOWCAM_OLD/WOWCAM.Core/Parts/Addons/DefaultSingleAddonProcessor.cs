﻿using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper.Parts;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class DefaultSingleAddonProcessor(IAppSettings appSettings, IWebViewWrapper webViewWrapper, ISmartUpdateFeature smartUpdateFeature) : ISingleAddonProcessor
    {
        private readonly IAppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        private readonly IWebViewWrapper webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
        private readonly ISmartUpdateFeature smartUpdateFeature = smartUpdateFeature ?? throw new ArgumentNullException(nameof(smartUpdateFeature));

        public async Task ProcessAddonAsync(string addonPageUrl, IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default)
        {
            // No ".ConfigureAwait(false)" here, cause otherwise the wrapped WebView's scheduler is not the correct one.
            // In general, the Microsoft WebView2 has to use the UI thread scheduler as its scheduler, to work properly.
            // Remember: This is also true for "ContinueWith()" blocks aka "code after await", even when it is a helper.

            var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonPageUrl);
            var downloadFolder = appSettings.Data.AddonDownloadFolder;
            var unzipFolder = appSettings.Data.AddonUnzipFolder;

            // Fetch JSON data

            cancellationToken.ThrowIfCancellationRequested();

            var json = await webViewWrapper.NavigateToPageAndExecuteJavaScriptAsync(addonPageUrl, CurseHelper.FetchJsonScript, cancellationToken);
            var jsonModel = CurseHelper.SerializeAddonPageJson(json);
            var downloadUrl = CurseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);
            var zipFile = jsonModel.FileName;

            progress?.Report(new AddonProgress(AddonState.FetchFinished, addonName, 0));

            // Handle SmartUpdate feature

            cancellationToken.ThrowIfCancellationRequested();

            if (smartUpdateFeature.AddonExists(addonName, downloadUrl, zipFile))
            {
                // Copy zip file

                smartUpdateFeature.DeployZipFile(addonName);

                progress?.Report(new AddonProgress(AddonState.DownloadFinishedBySmartUpdate, addonName, 100));
            }
            else
            {
                // Download zip file

                await webViewWrapper.NavigateAndDownloadFileAsync(downloadUrl, new Progress<WebView.DownloadProgress>(p =>
                {
                    var percent = CalcDownloadPercent(p.ReceivedBytes, p.TotalBytes);
                    progress?.Report(new AddonProgress(AddonState.DownloadProgress, addonName, percent));
                }),
                cancellationToken);

                progress?.Report(new AddonProgress(AddonState.DownloadFinished, addonName, 100));

                smartUpdateFeature.AddOrUpdateAddon(addonName, downloadUrl, zipFile);
            }

            // Extract zip file

            cancellationToken.ThrowIfCancellationRequested();

            var zipFilePath = Path.Combine(downloadFolder, zipFile);

            if (!await UnzipHelper.ValidateZipFileAsync(zipFilePath, cancellationToken))
            {
                throw new InvalidOperationException($"It seems the addon zip file ('{zipFile}') is corrupted, cause zip file validation failed.");
            }

            await UnzipHelper.ExtractZipFileAsync(zipFilePath, unzipFolder, cancellationToken);

            progress?.Report(new AddonProgress(AddonState.UnzipFinished, addonName, 100));
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
