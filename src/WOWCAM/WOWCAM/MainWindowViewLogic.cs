using System.Threading.Tasks;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private void HideControls()
        {
            button.Visibility = Visibility.Hidden;
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
        }

        private void HandleMode()
        {
            button.Visibility = Visibility.Hidden;
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            hyperlink1.Inlines.Clear();
            hyperlink2.Inlines.Clear();

            if (mode == OperatingMode.DownloadOnly)
            {
                button.Content = "_Download";
                hyperlink1.Inlines.Add("Download-Folder");
            }

            if (mode == OperatingMode.UnzipOnly)
            {
                button.Content = "_Unzip";
                hyperlink1.Inlines.Add("Source-Folder");
                hyperlink2.Inlines.Add("Dest-Folder");
                textBlockHyperlink2.Visibility = Visibility.Visible;
            }

            if (mode == OperatingMode.DownloadAndUnzip)
            {
                button.Width = 125;
                button.Content = "_Download & Unzip";
                hyperlink1.Inlines.Add("Download-Folder");
                hyperlink2.Inlines.Add("Unzip-Folder");
                textBlockHyperlink2.Visibility = Visibility.Visible;
            }

            if (mode == OperatingMode.Update)
            {
                button.Content = "_Update";
                hyperlink1.Inlines.Add("Update-Folder");
            }

            textBlockHyperlink1.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Visible;
        }

        private void ConfigureControls()
        {
            wpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            wpfHelper.DisableHyperlinkHoverEffect(hyperlink1);
            wpfHelper.DisableHyperlinkHoverEffect(hyperlink2);

            progressBar.Value = 75;
            progressBar.Visibility = Visibility.Visible;
        }

        private async Task ConfigureWebView()
        {
            await webView.EnsureCoreWebView2Async();

            webView.IsEnabled = false;
        }
    }
}
