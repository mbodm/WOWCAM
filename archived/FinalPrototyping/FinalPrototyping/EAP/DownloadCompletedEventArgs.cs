using System.ComponentModel;

namespace FinalPrototyping.EAP
{
    // I followed Microsoft's EAP pattern implementation and best practices:
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

    public delegate void DownloadCompletedEventHandler(object sender, DownloadCompletedEventArgs e);

    public class DownloadCompletedEventArgs(Exception? error, bool cancelled) : AsyncCompletedEventArgs(error, cancelled, null)
    {
    }
}
