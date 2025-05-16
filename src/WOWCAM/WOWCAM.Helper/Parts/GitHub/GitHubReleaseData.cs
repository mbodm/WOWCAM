namespace WOWCAM.Helper.Parts.GitHub
{
    public sealed record GitHubReleaseData(
        Version Version,
        string DownloadUrl,
        string FileName);
}
