namespace WOWCAM.Core
{
    public interface ICurseHelper
    {
        string FetchJsonScript { get; }

        bool IsAddonPageUrl(string url);
        bool IsInitialDownloadUrl(string url);
        bool IsRealDownloadUrl(string url);
        string GetAddonSlugNameFromAddonPageUrl(string url);
        ModelCurseHelperJson SerializeAddonPageJson(string json);
        string BuildInitialDownloadUrl(ulong projectId, ulong fileId);
    }
}
