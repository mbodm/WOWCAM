namespace WOWCAM.Core.Parts.Addons
{
    public interface ISmartUpdateFeature
    {
        Task LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        bool AddonExists(string addonName, string downloadUrl, string zipFile);
        Task AddOrUpdateAddonAsync(string addonName, string downloadUrl, string zipFile, string downloadFolder, CancellationToken cancellationToken = default);
        string GetZipFilePath(string addonName);
    }
}
