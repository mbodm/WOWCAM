﻿using WOWCAM.Helper;

namespace WOWCAM.Core
{
    public interface IUpdateManager
    {
        Task<ModelApplicationUpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(ModelApplicationUpdateData updateData,
            IProgress<ModelDownloadHelperProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
        Task<bool> ApplyUpdateAsync(CancellationToken cancellationToken = default);
    }
}
