using System;
using System.Windows;
using System.Windows.Documents;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Todo: Load settings.

            mode = OperatingMode.DownloadAndUnzip; // Fake

            HandleMode();

            ConfigureControls();

            await ConfigureWebView();

            if (mode != OperatingMode.UnzipOnly)
            {
                webView.Source = new Uri("https://www.curseforge.com/wow/addons/details");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Todo: Save settings.
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            wpfHelper.ShowInfo("Todo: Show config folder.");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                switch (mode)
                {
                    case OperatingMode.DownloadOnly:
                        if (hyperlink.Name == "hyperlink2") wpfHelper.ShowInfo("Todo: Show download folder.");
                        break;
                    case OperatingMode.UnzipOnly:
                        if (hyperlink.Name == "hyperlink1") wpfHelper.ShowInfo("Todo: Show unzip source folder.");
                        if (hyperlink.Name == "hyperlink2") wpfHelper.ShowInfo("Todo: Show unzip dest folder.");
                        break;
                    case OperatingMode.DownloadAndUnzip:
                        if (hyperlink.Name == "hyperlink1") wpfHelper.ShowInfo("Todo: Show download folder.");
                        if (hyperlink.Name == "hyperlink2") wpfHelper.ShowInfo("Todo: Show unzip folder.");
                        break;
                    case OperatingMode.Update:
                        if (hyperlink.Name == "hyperlink2") wpfHelper.ShowInfo("Todo: Show update folder.");
                        break;
                    default:
                        wpfHelper.ShowError("Todo: Given mode is not supported.");
                        break;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case OperatingMode.DownloadOnly:
                    wpfHelper.ShowInfo("Todo: Do download-only stuff.");
                    break;
                case OperatingMode.UnzipOnly:
                    wpfHelper.ShowInfo("Todo: Do unzip-only stuff");
                    break;
                case OperatingMode.DownloadAndUnzip:
                    wpfHelper.ShowInfo("Todo: Do download and unzip stuff.");
                    break;
                case OperatingMode.Update:
                    wpfHelper.ShowInfo("Todo: Do update (smart download and unzip) stuff.");
                    break;
                default:
                    wpfHelper.ShowError("Todo: Given mode is not supported.");
                    break;
            }
        }
    }
}
