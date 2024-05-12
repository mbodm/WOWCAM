namespace WOWCAM.Helpers
{
    public sealed record ModelAddonPageJson(
        bool IsValid,
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
