﻿namespace WOWCAM.Helper
{
    public sealed record DownloadHelperProgress(string Url, bool PreTransfer, long ReceivedBytes, long TotalBytes, bool TransferFinished);
}
