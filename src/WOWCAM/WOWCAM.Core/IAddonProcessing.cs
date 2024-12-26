namespace WOWCAM.Core
{
    public enum AddonProcessingProgressState
    {
        StartingFetch,
        FinishedFetch,
        StartingDownload,
        FinishedDownload,
        StartingUnzip,
        FinishedUnzip,
    }

    public sealed record AddonProcessingProgress(AddonProcessingProgressState State, string Addon);

    public interface IAddonProcessing
    {
        public Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<AddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
