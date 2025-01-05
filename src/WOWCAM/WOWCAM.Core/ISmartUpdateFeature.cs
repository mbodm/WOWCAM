namespace WOWCAM.Core
{
    public interface ISmartUpdateFeature
    {
        string Storage { get; } // Using such a generic term here since this could be a file/database/whatever
        bool StorageExists { get; }

        Task CreateStorageIfNotExistsAsync(CancellationToken cancellationToken = default);
        Task RemoveStorageIfExistsAsync(CancellationToken cancellationToken = default);
        Task<bool> ExactEntryExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default);
        Task AddOrUpdateEntryAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default);
    }
}
