using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly string mode = "Unzip";

        private readonly IHelper helper;

        public MainWindow(IHelper helper)
        {
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));

            InitializeComponent();

            HideControls();

            MinWidth = 1440 / 2;
            MinHeight = 900 / 2;
            Title = $"WOWCAM {helper.GetApplicationVersion()}";

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleMode(mode);
            ConfigureControls();
            ConfigureHyperlinks();
            await ConfigureWebView();

            if (mode != "Unzip")
            {
                webView.Source = new Uri("https://www.curseforge.com/wow/addons/details");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mode == "Download")
            {
                MessageBox.Show("Todo: Do download-only stuff.");
            }

            if (mode == "Unzip")
            {
                MessageBox.Show("Todo: Do unzip-only stuff");
            }

            if (mode == "Update")
            {
                MessageBox.Show("Todo: Do update stuff (download and unzip).");
            }
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Todo: Show config folder.");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                if (hyperlink.Name == "hyperlink1" && mode == "Download")
                {
                    MessageBox.Show("Todo: Show download folder.");
                }

                if (hyperlink.Name == "hyperlink1" && mode == "Unzip")
                {
                    MessageBox.Show("Todo: Show unzip dest folder.");
                }

                if (hyperlink.Name == "hyperlink2" && mode == "Unzip")
                {
                    MessageBox.Show("Todo: Show unzip source folder.");
                }

                if (hyperlink.Name == "hyperlink1" && mode == "Update")
                {
                    MessageBox.Show("Todo: Show update folder.");
                }
            }
        }

        private void HideControls()
        {
            button.Visibility = Visibility.Hidden;
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
        }

        private void ConfigureControls()
        {
            progressBar.Value = 75;
            progressBar.Visibility = Visibility.Visible;
        }

        private void ConfigureHyperlinks()
        {
            helper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            helper.DisableHyperlinkHoverEffect(hyperlink1);
            helper.DisableHyperlinkHoverEffect(hyperlink2);
        }

        private async Task ConfigureWebView()
        {
            await webView.EnsureCoreWebView2Async();

            webView.IsEnabled = false;
        }

        private void HandleMode(string mode)
        {
            button.Visibility = Visibility.Hidden;
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            hyperlink1.Inlines.Clear();
            hyperlink2.Inlines.Clear();

            if (mode == "Download")
            {
                button.Content = "_Download";
                hyperlink1.Inlines.Add("Download-Folder");
            }

            if (mode == "Unzip")
            {
                button.Content = "_Unzip";
                hyperlink1.Inlines.Add("Dest-Folder");
                hyperlink2.Inlines.Add("Source-Folder");
                textBlockHyperlink2.Visibility = Visibility.Visible;
            }

            if (mode == "Update")
            {
                button.Content = "_Update";
                hyperlink1.Inlines.Add("Update-Folder");
            }

            textBlockHyperlink1.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Visible;
        }
    }
}
