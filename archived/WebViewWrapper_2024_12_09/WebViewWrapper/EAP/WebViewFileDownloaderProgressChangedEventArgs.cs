using System.ComponentModel;

namespace WebViewWrapper.EAP
{
    public sealed class WebViewFileDownloaderProgressChangedEventArgs(int progressPercentage, object? userState) : ProgressChangedEventArgs(progressPercentage, userState)
    {
    }
}
