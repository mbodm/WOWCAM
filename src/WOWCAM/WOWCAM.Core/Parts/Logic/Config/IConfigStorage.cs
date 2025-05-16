namespace WOWCAM.Core.Parts.Logic.Config
{
    // Using an abstraction here since the config storage could be a file/database/whatever

    public interface IConfigStorage
    {
        string StorageInformation { get; }
        bool StorageExists { get; }

        Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default);
    }
}
