namespace WOWCAM.Core.Parts.Addons
{
    public interface ISingleAddonProcessor
    {
        public Task ProcessAddonAsync(string addonPageUrl, IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
