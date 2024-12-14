namespace WebViewWrapper.Scrap
{
    public interface ICurseScraper
    {
        Task<string> GetAddonDownloadUrlAsync(string addonPageUrl, CancellationToken cancellationToken = default);
    }
}
