﻿namespace WOWCAM.Helper.Parts.Curse
{
    public sealed record CurseAddonPageJson(
        ulong ProjectId,
        string ProjectName,
        string ProjectSlug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
