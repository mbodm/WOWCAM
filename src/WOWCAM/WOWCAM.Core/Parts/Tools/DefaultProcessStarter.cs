using System.Diagnostics;
using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Tools
{
    public sealed class DefaultProcessStarter(ILogger logger) : IProcessStarter
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
                Process.Start("notepad", logger.Storage);
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("Could not start Notepad.exe process to show log file (see log file for details).", e);
            }
        }
    }
}
