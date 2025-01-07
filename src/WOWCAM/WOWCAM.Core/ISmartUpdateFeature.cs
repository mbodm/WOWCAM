namespace WOWCAM.Core
{
    public interface ISmartUpdateFeature
    {
        string Storage { get; } // Using such a generic term here since this could be a file/database/whatever
        bool StorageExists { get; }

        Task SaveToStorageAsync(CancellationToken cancellationToken = default);
        Task LoadFromStorageIfExistsAsync(CancellationToken cancellationToken = default);
        Task RemoveStorageIfExistsAsync(CancellationToken cancellationToken = default);

        bool ExactEntryExists(string addonName, string downloadUrl);
        void AddOrUpdateEntry(string addonName, string downloadUrl);
    }
}
