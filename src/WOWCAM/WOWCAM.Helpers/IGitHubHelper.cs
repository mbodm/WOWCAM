namespace WOWCAM.Helpers
{
    public interface IGitHubHelper
    {
        Task<ModelGitHubLatestReleaseData> GetLatestReleaseData(CancellationToken cancellationToken = default);
    }
}
