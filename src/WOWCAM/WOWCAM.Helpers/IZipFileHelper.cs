namespace WOWCAM.Helpers
{
    public interface IZipFileHelper
    {
        Task<bool> ValidateZipFileAsync(string zipFile, CancellationToken cancellationToken = default);
        Task ExtractZipFileAsync(string zipFile, string destFolder, CancellationToken cancellationToken = default);
    }
}
