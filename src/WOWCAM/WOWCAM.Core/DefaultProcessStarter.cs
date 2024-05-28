﻿using System.Diagnostics;

namespace WOWCAM.Core
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
    }
}
