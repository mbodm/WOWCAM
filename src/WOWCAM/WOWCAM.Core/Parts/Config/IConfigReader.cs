namespace WOWCAM.Core.Parts.Config
{
    public interface IConfigReader
    {
        Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default);
    }
}
