using System.ComponentModel;

namespace WOWCAM.WebView
{
    public sealed class FetchCompletedEventArgs(string json, Exception? error, bool cancelled, object? userState) :
        AsyncCompletedEventArgs(error, cancelled, userState)
    {
        public string Json { get; init; } = json;
    }
}
