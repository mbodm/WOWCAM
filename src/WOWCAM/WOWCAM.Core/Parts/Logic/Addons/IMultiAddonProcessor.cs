namespace WOWCAM.Core.Parts.Logic.Addons
{
    public interface IMultiAddonProcessor
    {
        public Task<uint> ProcessAddonsAsync(IEnumerable<string> addonUrls, string downloadFolder, string unzipFolder,
            IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
