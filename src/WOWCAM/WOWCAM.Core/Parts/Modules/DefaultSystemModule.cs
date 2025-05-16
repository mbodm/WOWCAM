using System.Diagnostics;
using WOWCAM.Logging;

namespace WOWCAM.Core.Parts.Modules
{
    public sealed class DefaultSystemModule(ILogger logger) : ISystemModule
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void OpenFolderInExplorer(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException($"'{nameof(folder)}' cannot be null or whitespace.", nameof(folder));
            }

            try
            {
                Process.Start("explorer", folder);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not start Explorer.exe process to open folder (see log file for details).", e);
            }
        }

        public void ShowLogFileInNotepad()
        {
            try
            {
                Process.Start("notepad", logger.StorageInformation);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not start Notepad.exe process to show log file (see log file for details).", e);
            }
        }
    }
}
