namespace WOWCAM.Curse
{
    public interface IDownloadHelper
    {
        Task DownloadAddonsAsync(IEnumerable<string> downloadUrls, IProgress<ModelDownloadHelperProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
