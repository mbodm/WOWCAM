namespace WOWCAM.Helper
{
    public interface ICurseHelper
    {
        string FetchJsonScript { get; }

        bool IsAddonPageUrl(string url);
        bool IsInitialDownloadUrl(string url);
        bool IsRealDownloadUrl(string url);
        string GetAddonSlugNameFromAddonPageUrl(string url);
        ModelCurseAddonPageJson SerializeAddonPageJson(string json);
        string BuildInitialDownloadUrl(ulong projectId, ulong fileId);
    }
}
