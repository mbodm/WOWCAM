using System.ComponentModel;

namespace WOWCAM.WebView
{
    public sealed class FetchCompletedEventArgs(string realDownloadUrl, Exception? error, bool cancelled, object? userState) :
        AsyncCompletedEventArgs(error, cancelled, userState)
    {
        public string RealDownloadUrl { get; init; } = realDownloadUrl;
    }
}
