namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        public Version GetInstalledVersion();
        Task<bool> CheckForUpdates(CancellationToken cancellationToken = default);
        Task<bool> DownloadAndApplyUpdate(CancellationToken cancellationToken = default);
    }
}
