using System.ComponentModel;
using WebViewWrapper.EAP.Many;
using WebViewWrapper.Logger;
using WebViewWrapper.Provider;

namespace WebViewWrapper.TAP
{
    public sealed class DefaultWebViewFileDownloaderTAP(ILogger logger, IWebViewProvider webViewProvider)
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebViewProvider webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));

        private readonly WebViewFileDownloaderM eap = new();

        public Task DownloadFilesAsync(IEnumerable<string> downloadUrls, string destFolder,
            IProgress<string>? progress = default, CancellationToken cancellationToken = default)
        {
            // This method follows the typical "wrap EAP into TAP" pattern approach

            var tcs = new TaskCompletionSource();

            // DownloadFilesAsyncProgressChanged
            void DownloadFilesAsyncProgressChangedEventHandler(object? sender, ProgressChangedEventArgs e)
            {
                logger.Log("Todo");

                progress?.Report()
            }

            // DownloadFilesAsyncCompleted
            void DownloadFilesAsyncCompletedEventHandler(object? sender, AsyncCompletedEventArgs e)
            {
                logger.Log("Todo");

                if (sender is not WebViewFileDownloaderM senderWebViewFileDownloader)
                {
                    tcs.SetException(new InvalidOperationException("Todo"));
                    return;
                }

                if (e.Cancelled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }



                senderWebViewFileDownloader.DownloadFilesAsyncProgressChanged -= DownloadFilesAsyncProgressChangedEventHandler;
                senderWebViewFileDownloader.DownloadFilesAsyncCompleted -= DownloadFilesAsyncCompletedEventHandler;

                tcs.TrySetResult();
            }

            eap.DownloadFilesAsyncProgressChanged += DownloadFilesAsyncProgressChangedEventHandler;
            eap.DownloadFilesAsyncCompleted += DownloadFilesAsyncCompletedEventHandler;

            cancellationToken.Register(eap.CancelDownloadAddonsAsync);

            eap.DownloadFilesAsync(downloadUrls, destFolder);

            eap.DownloadFilesAsyncProgressChanged += Eap_DownloadFilesAsyncProgressChanged;

            return tcs.Task;
        }
    }
}
