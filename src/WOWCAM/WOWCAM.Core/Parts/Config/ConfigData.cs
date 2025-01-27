namespace WOWCAM.Core.Parts.Config
{
    public sealed record ConfigData(
        string ActiveProfile,
        string TempFolder,
        IEnumerable<string> ActiveOptions,
        string TargetFolder,
        IEnumerable<string> AddonUrls)
    {
        public static ConfigData Empty() => new(
            string.Empty,
            string.Empty,
            [],
            string.Empty,
            []);
    }
}
