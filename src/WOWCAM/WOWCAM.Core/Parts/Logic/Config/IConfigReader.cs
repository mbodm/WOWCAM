namespace WOWCAM.Core.Parts.Logic.Config
{
    public interface IConfigReader
    {
        Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default);
    }
}
