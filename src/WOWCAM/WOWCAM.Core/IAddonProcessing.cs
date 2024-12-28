namespace WOWCAM.Core
{
    public enum AddonProcessingProgressState
    {
        StartingFetch,
        FinishedFetch,
        StartingDownload,
        Downloading,
        FinishedDownload,
        StartingUnzip,
        FinishedUnzip,
    }

    public sealed record AddonProcessingProgress(AddonProcessingProgressState State, string AddonName, byte DownloadPercent);

    public interface IAddonProcessing
    {
        public Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<AddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
