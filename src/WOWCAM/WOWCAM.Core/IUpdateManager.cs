using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Version GetInstalledVersion();
        Task<bool> CheckForUpdates(CancellationToken cancellationToken = default);
        Task<bool> DownloadAndApplyUpdate(IProgress<ModelDownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
    }
}
