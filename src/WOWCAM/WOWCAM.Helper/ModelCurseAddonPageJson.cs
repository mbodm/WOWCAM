namespace WOWCAM.Helper
{
    public sealed record ModelCurseAddonPageJson(
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
