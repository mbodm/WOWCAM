namespace WOWCAM.Core
{
    public interface IPlatformHelper
    {
        void OpenWindowsExplorer(string arguments = "");
        Task DeleteAllZipFilesInFolderAsync(string folder, CancellationToken cancellationToken = default);
    }
}
