using System.ComponentModel;

namespace WOWCAM.WebView
{
    public sealed class DownloadCompletedEventArgs(Exception? error, bool cancelled, object? userState)
        : AsyncCompletedEventArgs(error, cancelled, userState)
    {
    }
}
