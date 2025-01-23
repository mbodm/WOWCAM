using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Update
{
    public interface IUpdateManager
    {
        Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        void ApplyUpdate();
        void RestartApplication();
        bool RemoveBakFile();
    }
}
