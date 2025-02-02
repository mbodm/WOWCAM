namespace WOWCAM.Core.Parts.Addons
{
    public interface IMultiAddonProcessor
    {
        public Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string targetFolder, string workFolder,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
