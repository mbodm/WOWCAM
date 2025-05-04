using WOWCAM.Helper.Parts;

namespace WOWCAM.Core.Parts.Update
{
    public interface IUpdateManager
    {
        Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task ApplyUpdateAsync(CancellationToken cancellationToken = default);
        void RestartApplication(uint delayInSeconds);
        Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default);
    }
}
