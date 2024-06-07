namespace WOWCAM.Core
{
    public interface ICurseHelper
    {
        string DisableScrollbarScript { get; }
        string HideCookiebarScript { get; }
        string GrabJsonScript { get; }

        bool IsAddonPageUrl(string url);
        bool IsFetchedDownloadUrl(string url);
        bool IsRedirectWithApiKeyUrl(string url);
        bool IsRealDownloadUrl(string url);

        string GetAddonSlugNameFromAddonPageUrl(string url);
        Version1CurseHelperJson SerializeAddonPageJson(string json);
        string BuildFetchedDownloadUrl(ulong projectId, ulong fileId);
    }
}
