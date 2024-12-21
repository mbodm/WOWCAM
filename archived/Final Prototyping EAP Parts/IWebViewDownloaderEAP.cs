namespace WOWCAM.WebView
{
    // I followed Microsoft's EAP pattern implementation and best practices:
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

    public interface IWebViewDownloaderEAP
    {
        public event DownloadCompletedEventHandler DownloadCompleted;
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public bool IsBusy { get; }

        public void DownloadAsync(IEnumerable<string> downloadUrls, string destFolder);
        void DownloadAsyncCancel();
    }
}
