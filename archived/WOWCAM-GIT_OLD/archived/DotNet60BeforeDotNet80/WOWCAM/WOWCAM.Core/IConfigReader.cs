namespace WOWCAM.Core
{
    public interface IConfigReader
    {
        string Storage { get; } // Using such a generic term here, since config could be a file, or database, or whatever.

        OperatingMode OperatingMode { get; }
        string LoadedProfile { get; }
        string TempFolder { get; }
        string DownloadFolder { get; }
        string UnzipFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        void ValidateConfig(bool downloadMode, bool unzipMode);
        void ReadConfig();
    }
}
