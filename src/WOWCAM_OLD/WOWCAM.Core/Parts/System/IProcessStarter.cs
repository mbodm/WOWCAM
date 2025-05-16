namespace WOWCAM.Core.Parts.System
{
    public interface IProcessStarter
    {
        void OpenFolderInExplorer(string folder);
        void ShowLogFileInNotepad();
    }
}
