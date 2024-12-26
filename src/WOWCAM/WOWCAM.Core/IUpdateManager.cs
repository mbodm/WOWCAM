using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public sealed record UpdateManagerUpdateData(
    Version InstalledVersion,
    Version AvailableVersion,
    bool UpdateAvailable,
    string UpdateDownloadUrl,
    string UpdateFileName);

    public interface IUpdateManager
    {
        Task<UpdateManagerUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateManagerUpdateData updateData, IProgress<DownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        void ApplyUpdate();
        void RestartApplication();
        bool RemoveBakFile();
    }
}
