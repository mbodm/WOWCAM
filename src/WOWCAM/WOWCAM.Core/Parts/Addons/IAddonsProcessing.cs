namespace WOWCAM.Core.Parts.Addons
{
    public interface IAddonsProcessing
    {
        public Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string targetFolder, string workFolder, bool showDownloadDialog = false,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
