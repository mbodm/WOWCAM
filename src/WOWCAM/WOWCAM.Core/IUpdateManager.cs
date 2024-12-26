using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<UpdateManagerUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateManagerUpdateData updateData, IProgress<DownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        void ApplyUpdate();
        void RestartApplication();
        bool RemoveBakFile();
    }
}
