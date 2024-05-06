using System.ComponentModel;

namespace WOWCAM.WebView
{
    public sealed class FetchCompletedEventArgs(string addonPageJson, Exception? error, bool cancelled, object? userState) :
        AsyncCompletedEventArgs(error, cancelled, userState)
    {
        public string AddonPageJson { get; init; } = addonPageJson;
    }
}
