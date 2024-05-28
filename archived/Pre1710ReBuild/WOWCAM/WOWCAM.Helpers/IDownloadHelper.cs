namespace WOWCAM.Helpers
{
    public interface IDownloadHelper
    {
        Task DownloadFileAsync(string downloadUrl, string filePath,
            IProgress<ModelDownloadHelperProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
