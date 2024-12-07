using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(ModelApplicationUpdateData updateData, IProgress<DownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        void ApplyUpdate();
        void RestartApplication();
        bool RemoveBakFile();
    }
}
