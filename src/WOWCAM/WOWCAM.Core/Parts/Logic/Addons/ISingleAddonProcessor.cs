namespace WOWCAM.Core.Parts.Logic.Addons
{
    public interface ISingleAddonProcessor
    {
        public Task ProcessAddonAsync(string addonUrl, string downloadFolder, string unzipFolder,
            IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
