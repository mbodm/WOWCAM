using System.Diagnostics;

namespace WOWCAM.Core
{
    public sealed class DefaultProcessHelper(ILogger logger) : IProcessHelper
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

        public void StartUpdater(string applicationFolder)
        {
            if (string.IsNullOrWhiteSpace(applicationFolder))
            {
                throw new ArgumentException($"'{nameof(applicationFolder)}' cannot be null or whitespace.", nameof(applicationFolder));
            }

            var updaterName = "WOWCAMUPD.exe";
            var updaterPath = Path.Combine(applicationFolder, updaterName);

            try
            {
                if (!File.Exists(updaterPath))
                {
                    throw new InvalidOperationException($"{updaterName} not exists in application folder.");
                }

                Process.Start(updaterPath);
            }
            catch (Exception e)
            {
                logger.Log(e);

                throw new InvalidOperationException($"Could not start {updaterName} process to update application (see log file for details).", e);
            }
        }
    }
}
