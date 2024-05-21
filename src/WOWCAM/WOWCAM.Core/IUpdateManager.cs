using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        void PrepareForDownload();
        Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task PrepareForUpdateAsync(CancellationToken cancellationToken = default);
        void StartUpdateAppWithAdminRights();
    }
}
