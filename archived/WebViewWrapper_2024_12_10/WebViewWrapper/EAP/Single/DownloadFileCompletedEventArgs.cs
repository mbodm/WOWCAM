using System.ComponentModel;

namespace WebViewWrapper.EAP.Single
{
    // I followed Microsoft's EAP pattern implementation and best practices:
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

    public delegate void DownloadFileCompletedEventHandler(object sender, DownloadFileCompletedEventArgs e);

    public class DownloadFileCompletedEventArgs : AsyncCompletedEventArgs
    {
        public DownloadFileCompletedEventArgs(Exception? error, bool cancelled, object? userState) : base(error, cancelled, userState)
        {
        }
    }
}
