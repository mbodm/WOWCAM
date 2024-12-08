using System.ComponentModel;

namespace WebViewWrapper
{
    public sealed class WebViewHelperProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public WebViewHelperProgressChangedEventArgs(int progressPercentage, object? userState) : base(progressPercentage, userState)
        {
        }

        public WebViewHelperProgressChangedEventArgs(int progressPercentage, object? userState, WebViewWrapperDownloadProgress? progressData) : base(progressPercentage, userState)
        {
            ProgressData = progressData;
        }

        public WebViewWrapperDownloadProgress? ProgressData { get; }
    }
}
