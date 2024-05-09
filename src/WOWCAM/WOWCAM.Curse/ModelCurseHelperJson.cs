namespace WOWCAM.Curse
{
    public sealed record ModelCurseHelperJson(
        bool IsValid,
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
