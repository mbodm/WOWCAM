namespace WOWCAM.Helper.Parts
{
    public sealed record GitHubReleaseData(
        Version Version,
        string DownloadUrl,
        string FileName);
}
