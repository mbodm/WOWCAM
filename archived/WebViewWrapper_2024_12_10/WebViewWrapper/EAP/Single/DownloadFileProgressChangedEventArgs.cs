using System.ComponentModel;

namespace WebViewWrapper.EAP.Single
{
    // I followed Microsoft's EAP pattern implementation and best practices:
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

    public delegate void DownloadFileProgressChangedEventHandler(object sender, DownloadFileProgressChangedEventArgs e);

    public class DownloadFileProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public DownloadFileProgressChangedEventArgs(int progressPercentage, object? userState) : base(progressPercentage, userState)
        {
        }
    }
}
