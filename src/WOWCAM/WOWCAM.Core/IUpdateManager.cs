namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<bool> CheckForUpdates(CancellationToken cancellationToken = default);
        Task DownloadAndApplyUpdate(CancellationToken cancellationToken = default);
    }
}
