namespace WOWCAM.Core.Parts.Logic.Config
{
    public sealed record ConfigData(
        string ActiveProfile,
        string Theme,
        string TempFolder,
        IEnumerable<string> ActiveOptions,
        string TargetFolder,
        IEnumerable<string> AddonUrls)
    {
        public static ConfigData Empty() => new(
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            string.Empty,
            []);
    }
}
