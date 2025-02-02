namespace WOWCAM.Core.Parts.Addons
{
    public interface IAddonsProcessing
    {
        public Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
    }
}
