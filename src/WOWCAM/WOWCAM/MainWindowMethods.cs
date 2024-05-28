using System.Windows;
using System.Windows.Documents;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
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
            border.Visibility = Visibility.Visible;
            webView.IsEnabled = true;
        }

        private void SetControls(bool enabled)
        {
            textBlockConfigFolder.IsEnabled = enabled;
            textBlockCheckUpdates.IsEnabled = enabled;
            labelProgressBar.IsEnabled = enabled;
            progressBar.IsEnabled = enabled;
            button.IsEnabled = enabled;

            if (enabled)
            {
                DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
                DisableHyperlinkHoverEffect(hyperlinkCheckUpdates);
            }
        }

        private void EnableConfigFolderHyperlink()
        {
            textBlockConfigFolder.IsEnabled = true;
            DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
        }

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

            var environment = await webViewHelper.CreateEnvironmentAsync(config.TempFolder);
            await webView.EnsureCoreWebView2Async(environment);
        }

        private static void DisableHyperlinkHoverEffect(Hyperlink hyperlink)
        {
            // By default a Hyperlink has a hover effect: The foreground color is changed on mouse hover.
            // Since i don´t want that behaviour and since Hyperlink is somewhat "special" in WPF and a
            // bit painful to style, i use a little trick here: I just set the Foreground property. This
            // prevents the Hyperlink from using the default hover color (red). Result: Effect disabled.

            hyperlink.Foreground = hyperlink.Foreground;
        }

        private void PreUpdateCheck()
        {
            SetControls(false);

            labelProgressBar.IsEnabled = true;
            progressBar.IsEnabled = true;

            labelProgressBar.Content = "Check for updates";
            progressBar.Maximum = 1;
            progressBar.Value = 0;
        }

        private void PostUpdateCheck()
        {
            SetControls(true);

            labelProgressBar.Content = string.Empty;
            progressBar.Maximum = 1;
            progressBar.Value = 0;
        }

        private void PreAddonProcessing()
        {
            SetControls(false);

            labelProgressBar.IsEnabled = true;
            progressBar.IsEnabled = true;

            labelProgressBar.Content = string.Empty;
            progressBar.Value = 0;
            progressBar.Maximum = config.AddonUrls.Count() * 3;
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
