namespace WOWCAM.Core.Parts.Modules
{
    public interface IAppSettings
    {
        string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever
        SettingsData AppSettings { get; }

        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
