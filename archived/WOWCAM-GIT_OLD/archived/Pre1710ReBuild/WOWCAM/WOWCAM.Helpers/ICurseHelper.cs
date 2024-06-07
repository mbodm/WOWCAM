namespace WOWCAM.Helpers
{
    public interface ICurseHelper
    {
        string FetchJsonScript { get; }

        bool IsAddonPageUrl(string url);
        bool IsInitialDownloadUrl(string url);
        bool IsRealDownloadUrl(string url);
        string GetAddonSlugNameFromAddonPageUrl(string url);
        ModelAddonPageJson SerializeAddonPageJson(string json);
        string BuildInitialDownloadUrl(ulong projectId, ulong fileId);
    }
}
