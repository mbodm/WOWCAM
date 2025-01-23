namespace WOWCAM.Core.Parts.Addons
{
    public interface IAddonProcessing
    {
        public Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string targetFolder, string workFolder, bool showDownloadDialog = false, bool smartUpdate = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
