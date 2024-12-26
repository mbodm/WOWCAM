namespace WOWCAM.WebView
{
    public interface IAddonProcessing
    {
        public Task ProcessAddonsAsync(IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<ModelAddonProcessingProgress>? progress = default, CancellationToken cancellationToken = default);
    }
}
