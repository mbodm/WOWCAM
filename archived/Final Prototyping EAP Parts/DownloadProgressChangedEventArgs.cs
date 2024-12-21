using System.ComponentModel;

namespace WOWCAM.WebView
{
    // I followed Microsoft's EAP pattern implementation and best practices:
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

    public delegate void DownloadProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);

    public class DownloadProgressChangedEventArgs(int progressPercentage) : ProgressChangedEventArgs(progressPercentage, null)
    {
    }
}
