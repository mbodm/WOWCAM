namespace WOWCAM.Core.Parts.Addons
{
    public interface IAddonsModule
    {
        public Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
