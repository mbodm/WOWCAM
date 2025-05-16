namespace WOWCAM.Core.Parts.Logic.Addons
{
    public interface ISmartUpdateFeature
    {
        Task InitAsync(string smartUpdateFolder, string addonsDownloadFolder, CancellationToken cancellationToken = default);
        Task LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        bool AddonExists(string addonName, string downloadUrl, string zipFile);
        void AddOrUpdateAddon(string addonName, string downloadUrl, string zipFile);
        void DeployZipFile(string addonName);
    }
}
