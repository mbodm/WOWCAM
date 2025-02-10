using System.Windows;
using System.Windows.Documents;
using WOWCAM.Core.Parts.WebView;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private async Task ConfigureWebViewAsync(string webViewUserDataFolder)
        {
            // The "CoreWebView2InitializationCompleted" event will be invoked BY the "EnsureCoreWebView2Async()" method and BEFORE its task returns.
            // The MSDN documentation just says "invoked", but does not state if the task does also wait until the event handler has finished or not.
            // Therefore we better use a "TaskCompletionSource" here, for our own task, to tell our app initialization if we should stop or continue.
            // Sadly we can not do this part anywhere else outside, since we need access to the WebView2's WPF (or WinForms) encapsulation component.
            // See the "Remarks" section here:
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.wpf.webview2
            // See the "Returns" section here:
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.wpf.webview2.ensurecorewebview2async

            var tcs = new TaskCompletionSource();
            webView.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (e.IsSuccess)
                {
                    tcs.TrySetResult();
                }
                else
                {
                    if (e.InitializationException == null)
                    {
                        tcs.TrySetException(new InvalidOperationException($"The WebView2 'CoreWebView2InitializationCompleted' event was invoked, but 'IsSuccess' was false."));
                    }
                    else
                    {
                        tcs.TrySetException(e.InitializationException);
                    }
                }
            };

            try
            {
                var environment = await WebViewEnvironment.CreateAsync(webViewUserDataFolder);
                await webView.EnsureCoreWebView2Async(environment);
                await tcs.Task;
            }
            catch (Exception e)
            {
                logger.Log(e);
                throw new InvalidOperationException("WebView2 initialization failed (see log file for details).");
            }
        }

        private void SetControls(bool enabled)
        {
            SetLink(0, enabled);
            SetLink(1, enabled);
            SetProgress(enabled, null, null, null);
            button.IsEnabled = enabled;
        }

        private void ShowWebView()
        {
            // All sizes are based on 16:10 format relations (in example 1440x900)

            Width = 1280;
            Height = 800;
            MinWidth = 640;
            MinHeight = 400;
            Left = (SystemParameters.PrimaryScreenWidth / 2) - (Width / 2);
            Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);
            ResizeMode = ResizeMode.CanResize;

            webView.Width = double.NaN;
            webView.Height = double.NaN;
            webView.IsEnabled = true;
            border.IsEnabled = true;
            border.Visibility = Visibility.Visible;
        }

        private void SetLink(uint target, bool enabled)
        {
            // target == 0 -> config folder link
            // target == 1 -> check updates link

            if (target == 0 && linksPanel.Children.Contains(textBlockConfigFolder))
            {
                hyperlinkConfigFolder.IsEnabled = enabled;
                DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            }

            if (target == 1 && linksPanel.Children.Contains(textBlockCheckUpdates))
            {
                hyperlinkCheckUpdates.IsEnabled = enabled;
                DisableHyperlinkHoverEffect(hyperlinkCheckUpdates);
            }
        }

        private void RemoveLink(uint target)
        {
            // target == 0 -> config folder link
            // target == 1 -> check updates link

            if (target == 0 && linksPanel.Children.Contains(textBlockConfigFolder))
            {
                linksPanel.Children.Remove(textBlockConfigFolder);
            }

            if (target == 1 && linksPanel.Children.Contains(textBlockCheckUpdates))
            {
                linksPanel.Children.Remove(textBlockCheckUpdates);
            }
        }

        private void SetProgress(bool? enabled, string? text, double? value, double? maximum)
        {
            if (text != null) labelProgressBar.Content = text;
            if (value != null) progressBar.Value = value.Value;
            if (maximum != null) progressBar.Maximum = maximum.Value;

            if (enabled != null)
            {
                labelProgressBar.IsEnabled = enabled.Value;
                progressBar.IsEnabled = enabled.Value;
            }
        }

        // Hyperlink control in WPF has some default hover effect which sets foreground color to red.
        // Since i don´t want that behaviour and since Hyperlink is somewhat painful to style in WPF,
        // i just set the correct default system colors manually, when Hyperlink is enabled/disabled.
        // Note: This is just some temporary fix anyway, cause of the upcoming theme-support changes.
        private static void DisableHyperlinkHoverEffect(Hyperlink hyperlink) =>
            hyperlink.Foreground = hyperlink.IsEnabled ? SystemColors.HotTrackBrush : SystemColors.GrayTextBrush;

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
