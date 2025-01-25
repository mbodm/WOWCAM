namespace WOWCAM.Core.Parts.Addons
{
    public interface IAddonProcessing
    {
        public Task ProcessAddonAsync(string addonPageUrl, string downloadFolder, string unzipFolder,
            IProgress<AddonProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
