namespace WOWCAM.Helpers
{
    public sealed record ModelGitHubLatestReleaseData(
        Version Version,
        string DownloadUrl,
        string FileName);
}
