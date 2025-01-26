namespace WOWCAM.Core.Parts.Config
{
    public interface IConfigModule
    {
        ConfigData Data { get; }

        string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever
        bool StorageExists { get; }

        Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default);
        Task LoadFromStorageAsync(CancellationToken cancellationToken = default);
        void Validate();
    }
}
