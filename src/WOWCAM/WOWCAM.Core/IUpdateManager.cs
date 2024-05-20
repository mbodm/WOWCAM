using WOWCAM.Helpers;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task StartUpdaterWithAdminRightsAsync(Action restartApplicationAction);
        Task SelfUpdateIfRequestedAsync(Action restartApplicationAction, CancellationToken cancellationToken = default);
    }
}
