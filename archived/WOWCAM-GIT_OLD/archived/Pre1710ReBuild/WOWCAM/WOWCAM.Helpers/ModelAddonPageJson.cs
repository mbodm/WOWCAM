namespace WOWCAM.Helpers
{
    public sealed record ModelAddonPageJson(
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
