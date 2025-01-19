namespace WOWCAM.Core
{
    public interface IConfig
    {
        string ActiveProfile { get; }
        string TempFolder { get; }
        IEnumerable<string> ActiveOptions { get; }

        string TargetFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        string Storage { get; } // Using such a generic term here since this could be a file/database/whatever
        bool Exists { get; }

        Task CreateDefaultAsync(CancellationToken cancellationToken = default);
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
