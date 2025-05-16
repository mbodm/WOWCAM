namespace WOWCAM.Core.Parts.Modules
{
    public interface ISettingsModule
    {
        string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever
        SettingsData SettingsData { get; }

        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
