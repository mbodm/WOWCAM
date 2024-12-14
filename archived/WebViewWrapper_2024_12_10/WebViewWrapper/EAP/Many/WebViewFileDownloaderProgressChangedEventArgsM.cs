using System.ComponentModel;

namespace WebViewWrapper.EAP.Many
{
    public sealed class WebViewFileDownloaderProgressChangedEventArgsM(int progressPercentage, object? userState) : ProgressChangedEventArgs(progressPercentage, userState)
    {
    }
}
