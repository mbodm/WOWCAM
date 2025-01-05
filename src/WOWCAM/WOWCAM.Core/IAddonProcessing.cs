namespace WOWCAM.Core
{
    public interface IAddonProcessing
    {
        public Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder, bool showDownloadDialog = false, bool smartUpdate = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
