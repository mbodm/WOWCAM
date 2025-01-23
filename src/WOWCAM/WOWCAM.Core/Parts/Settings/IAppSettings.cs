namespace WOWCAM.Core.Parts.Settings
{
    public interface IAppSettings
    {
        AppSettingsData Data { get; }

        void Init();
    }
}
