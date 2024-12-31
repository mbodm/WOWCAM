namespace WOWCAM.Core
{
    public interface IAddonProcessing
    {
        public Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder, bool smartUpdate = false, bool showDownloadDialog = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
