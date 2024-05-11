namespace WOWCAM.Core
{
    public interface IProcessHelper
    {
        void OpenFolderInExplorer(string folder);
        void StartUpdater(string applicationFolder);
    }
}
