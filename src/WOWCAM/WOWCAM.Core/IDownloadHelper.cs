namespace WOWCAM.Core
{
    public interface IDownloadHelper
    {
        Task DownloadAddonAsync(string downloadUrl, string filePath, CancellationToken cancellationToken = default);
    }
}
