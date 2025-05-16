using WOWCAM.Core.Parts.Logic.Update;
using WOWCAM.Helper.Parts.Download;

namespace WOWCAM.Core.Parts.Modules
{
    public interface IUpdateModule
    {
        Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task ApplyUpdateAndRestartApplicationAsync(CancellationToken cancellationToken = default);
        Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default);
    }
}
