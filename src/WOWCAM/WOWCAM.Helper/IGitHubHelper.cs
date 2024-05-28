namespace WOWCAM.Helper
{
    public interface IGitHubHelper
    {
        Task<ModelGitHubReleaseData> GetLatestReleaseData(CancellationToken cancellationToken = default);
    }
}
