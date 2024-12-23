namespace WOWCAM.WebView
{
    public enum EnumAddonProcessingState
    {
        StartingFetch,
        FinishedFetch,
        StartingDownload,
        FinishedDownload,
        StartingUnzip,
        FinishedUnzip,
    }

    public sealed record ModelAddonProcessingProgress(EnumAddonProcessingState State, string Addon);
}
