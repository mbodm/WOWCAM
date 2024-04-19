namespace WOWCAM.Core
{
    public interface IConfig
    {
        string Storage { get; } // Using such a generic term here, since this could be a file, or database, or whatever.

        string ActiveProfile { get; }
        string TempFolder { get; }
        OperatingMode OperatingMode { get; }
        string DownloadFolder { get; }
        string UnzipFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        bool Exists();
        Task CreateEmptyAsync(CancellationToken cancellationToken = default);
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
