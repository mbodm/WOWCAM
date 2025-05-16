using WOWCAM.Helper.Parts.Download;

namespace WOWCAM.Core.Parts.Logic.Update
{
    public interface IUpdateManager
    {
        Task InitAsync(string pathToApplicationTempFolder, CancellationToken cancellationToken = default);
        Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task ApplyUpdateAsync(CancellationToken cancellationToken = default);
        void RestartApplication(uint delayInSeconds);
        Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default);
    }
}
