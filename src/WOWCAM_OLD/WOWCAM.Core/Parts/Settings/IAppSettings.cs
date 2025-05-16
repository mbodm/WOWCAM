namespace WOWCAM.Core.Parts.Settings
{
    public interface IAppSettings
    {
        string ConfigStorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever
        SettingsData Data { get; }

        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
