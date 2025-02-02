namespace WOWCAM.Core.Parts.Addons
{
    public interface ISingleAddonProcessor
    {
        public Task ProcessAddonAsync(string addonPageUrl, string downloadFolder, string unzipFolder,
            IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
