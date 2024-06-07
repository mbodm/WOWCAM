﻿namespace WOWCAM.Core
{
    public interface IConfigReader
    {
        string Storage { get; } // Using such generic term here, since config could be a file, or database, or whatever.

        IEnumerable<string> AddonUrls { get; }
        string DownloadFolder { get; }
        string UnzipFolder { get; }
        string TempFolder { get; }

        void ReadConfig();
        void ValidateConfig(bool downloadMode, bool unzipMode);
    }
}
