namespace WOWCAM.Core
{
    public interface IUnzipHelper
    {
        Task ExtractZipFilesAsync(string sourceFolder, string destFolder, IProgress<ModelUnzipHelperProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
