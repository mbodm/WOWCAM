namespace WOWCAM.Core.Parts.Settings
{
    public interface IAppSettings
    {
        AppSettingsData Data { get; }

        Task InitAsync(CancellationToken cancellationToken = default);
    }
}
