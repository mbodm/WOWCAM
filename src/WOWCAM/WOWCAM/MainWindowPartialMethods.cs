using System.Windows;
using System.Windows.Documents;
using WOWCAM.Core.Parts.WebView;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private async Task ConfigureWebViewAsync()
        {
            webView.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (!e.IsSuccess)
                {
                    logger.Log($"WebView2 initialization failed (the event's exception message was '{e.InitializationException.Message}').");
                    ShowError("WebView2 initialization failed (see log file for details).");
                }
            };

            var environment = await WebViewHelper.CreateEnvironmentAsync(appSettings.Data.WebViewEnvironmentFolder);
            await webView.EnsureCoreWebView2Async(environment);
        }

        private void SetControls(bool enabled)
        {
            Setlinks(enabled);
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

        private void Setlinks(bool enabled, uint target = 2)
        {
            // target == 0 -> config folder link
            // target == 1 -> check updates link
            // target == 2 -> both links

            if (target == 0 || target == 2)
            {
                //textBlockConfigFolder.IsEnabled = enabled;
                hyperlinkConfigFolder.IsEnabled = enabled;
                DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            }

            if (target == 1 || target == 2)
            {
                //textBlockCheckUpdates.IsEnabled = enabled;
                hyperlinkCheckUpdates.IsEnabled = enabled;
                DisableHyperlinkHoverEffect(hyperlinkCheckUpdates);
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
