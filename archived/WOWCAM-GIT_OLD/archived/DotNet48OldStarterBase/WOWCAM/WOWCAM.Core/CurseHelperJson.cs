namespace WOWCAM.Core
{
    public sealed record Version1CurseHelperJson(
        bool IsValid,
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
