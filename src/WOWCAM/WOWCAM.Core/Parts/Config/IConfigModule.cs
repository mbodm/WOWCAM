namespace WOWCAM.Core.Parts.Config
{
    public interface IConfigModule
    {
        string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever
        AppSettings AppSettings { get; }

        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
