﻿namespace WOWCAM.Core
{
    public interface IConfig
    {
        string ActiveProfile { get; }
        string ApplicationMode { get; }
        string TempFolder { get; }
        string TargetFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        string Storage { get; } // Using such a generic term here, since this could be a file, or database, or whatever.
        bool Exists { get; }

        Task CreateEmptyAsync(CancellationToken cancellationToken = default);
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
