namespace WOWCAM.Core.Parts.Addons
{
    public interface ISmartUpdateFeature
    {
        Task LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        bool AddonExists(string addonName, string downloadUrl, string zipFile);
        void AddOrUpdateAddon(string addonName, string downloadUrl, string zipFile);
        void DeployZipFile(string addonName);
    }
}
