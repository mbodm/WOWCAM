namespace WOWCAM.Helper
{
    public interface IFileSystemHelper
    {
        bool IsValidAbsolutePath(string path);
        Task DeleteFolderContentAsync(string folder, CancellationToken cancellationToken = default);
        Task MoveFolderContentAsync(string sourceFolder, string destFolder, CancellationToken cancellationToken = default);
        Version GetExeFileVersion(string filePath);
    }
}
