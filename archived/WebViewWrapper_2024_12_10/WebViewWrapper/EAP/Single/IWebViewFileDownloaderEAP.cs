namespace WebViewWrapper.EAP.Single
{
    public interface IWebViewFileDownloaderEAP
    {
        public event DownloadFileCompletedEventHandler DownloadFileCompleted;
        public event DownloadFileProgressChangedEventHandler DownloadFileProgressChanged;

        public void DownloadFileAsync(string downloadUrl, string destFolder, object? userState);
        void DownloadFileAsyncCancel(object userState);
    }
}
