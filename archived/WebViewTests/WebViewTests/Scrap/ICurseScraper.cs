namespace WebViewTests.Scrap
{
    public interface ICurseScraper
    {
        Task<string> GetAddonDownloadUrlAsync(string addonPageUrl, CancellationToken cancellationToken = default);
    }
}
