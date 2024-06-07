using System.Diagnostics;

namespace WOWCAM.Core
{
    public sealed class DefaultProcessHelper(ILogger logger) : IProcessHelper
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void OpenFolderInExplorer(string folder)
        {
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
    }
}
