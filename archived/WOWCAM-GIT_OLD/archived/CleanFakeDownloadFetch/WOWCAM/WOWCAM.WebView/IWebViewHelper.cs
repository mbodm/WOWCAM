using Microsoft.Web.WebView2.Core;

namespace WOWCAM.WebView
{
    public interface IWebViewHelper
    {
        // Not using the TAP pattern here, to encapsulate the EAP pattern (on which WebView2 is built on). On purpose! And here is why:
        // It is technically possible to wrap the TAP pattern around the EAP pattern, with some help of the TaskCompletionSource class.
        // This would be possible here too. But the WebView2 is built on the EAP pattern, to naturally fit the event-driven concepts of
        // a WinForms or WPF application. Using the TAP pattern here would work somewhat against that natural direction. But even when
        // this makes some sense, it is not really a problem or the reason why the TAP pattern is a bad idea here. The real problem is:
        // Since the stuff here shall act as a wrapper (around the WebView2 WinForms/WPF component), the wrapper will have shared state.
        // A typical "async Task DownloadAsync()" TAP method would make a user believe he is able to call that TAP method concurrently,
        // in example in some Task.WhenAll() environment. But this is not true at all! In fact the TAP method would even have to use a
        // boolean lock flag, to protect itself from being started again, if already running. Cause otherwise there would be more than
        // one access to the same shared WebView2 component, at the same time. Which would end up in some unpredictable and error prone
        // behaviour, which will lead to a crashing WebView2 component. The bottom line is: Such a TAP approach would make it necessary
        // to instantiate the WebView2 inside that TAP method. All of this just makes no sense for the given use case. Therefore using
        // the TAP pattern is not beneficial at all here. Therefore using a typical EAP approach here, by following this best practices:
        // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/best-practices-for-implementing-the-event-based-asynchronous-pattern

        event EventHandler<FetchCompletedEventArgs> FetchCompleted;

        bool IsInitialized { get; }
        bool IsFetching { get; }

        Task<CoreWebView2Environment> CreateEnvironmentAsync(string tempFolder);
        void Initialize(CoreWebView2 coreWebView, string downloadFolder);
        void FetchAsync(string addonUrl);
    }
}
