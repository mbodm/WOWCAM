using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Addons
{
    public sealed class AddonsModule : IAddonsModule
    {
        public Task<uint> ProcessAddonsAsync(IProgress<byte>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
