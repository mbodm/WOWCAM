namespace WOWCAM.Core.Parts.Addons
{
    public interface IMultiAddonProcessor
    {
        public Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
