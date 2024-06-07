using System.IO;
using System.Windows;
using System.Windows.Documents;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly IConfig config;

        public MainWindow(IConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            InitializeComponent();

            // 16:10 format (1440x900 fits Curse site better than 1280x800)
            Width = 1440;
            Height = 900;
            MinWidth = 1440 / 2;
            MinHeight = 900 / 2;

            Title = $"WOWCAM {AppHelper.GetApplicationVersion()}";

            InitControls();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!config.Exists())
                {
                    await config.CreateEmptyAsync();
                }

                await config.LoadAsync();
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);
                return;
            }



            ConfigureControls();



            await ConfigureWebView();
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            WpfHelper.ShowInfo("Todo: Show config folder.");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                switch (config.OperatingMode)
                {
                    case OperatingMode.DownloadOnly:
                        if (hyperlink.Name == "hyperlink2") WpfHelper.ShowInfo("Todo: Show download folder.");
                        break;
                    case OperatingMode.UnzipOnly:
                        if (hyperlink.Name == "hyperlink1") WpfHelper.ShowInfo("Todo: Show unzip source folder.");
                        if (hyperlink.Name == "hyperlink2") WpfHelper.ShowInfo("Todo: Show unzip dest folder.");
                        break;
                    case OperatingMode.DownloadAndUnzip:
                        if (hyperlink.Name == "hyperlink1") WpfHelper.ShowInfo("Todo: Show download folder.");
                        if (hyperlink.Name == "hyperlink2") WpfHelper.ShowInfo("Todo: Show unzip folder.");
                        break;
                    case OperatingMode.SmartUpdate:
                        if (hyperlink.Name == "hyperlink2") WpfHelper.ShowInfo("Todo: Show update folder.");
                        break;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (config.OperatingMode)
            {
                case OperatingMode.DownloadOnly:
                    WpfHelper.ShowInfo("Todo: Do download-only stuff.");
                    break;
                case OperatingMode.UnzipOnly:
                    WpfHelper.ShowInfo("Todo: Do unzip-only stuff");
                    break;
                case OperatingMode.DownloadAndUnzip:
                    WpfHelper.ShowInfo("Todo: Do download and unzip stuff.");
                    break;
                case OperatingMode.SmartUpdate:
                    WpfHelper.ShowInfo("Todo: Do update (smart download and unzip) stuff.");
                    break;
            }
        }

        private void InitControls()
        {
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            textBlockProgressBar.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
            button.Visibility = Visibility.Hidden;
            button.IsEnabled = false;
        }

        private void ConfigureControls()
        {
            hyperlink1.Inlines.Clear();
            hyperlink2.Inlines.Clear();

            switch (config.OperatingMode)
            {
                case OperatingMode.DownloadOnly:
                    button.Content = "_Download";
                    hyperlink2.Inlines.Add("Download-Folder");
                    if (CheckDownloadFolder())
                    {
                        textBlockHyperlink2.Visibility = Visibility.Visible;
                        button.IsEnabled = true;
                    }
                    break;
                case OperatingMode.UnzipOnly:
                    button.Content = "_Unzip";
                    hyperlink1.Inlines.Add("Source-Folder");
                    hyperlink2.Inlines.Add("Dest-Folder");
                    if (CheckDownloadFolder() && CheckUnzipFolder())
                    {
                        textBlockHyperlink1.Visibility = Visibility.Visible;
                        textBlockHyperlink2.Visibility = Visibility.Visible;
                        button.IsEnabled = true;
                    }
                    break;
                case OperatingMode.DownloadAndUnzip:
                    button.Width = 125;
                    button.Content = "_Download & Unzip";
                    hyperlink1.Inlines.Add("Download-Folder");
                    hyperlink2.Inlines.Add("Unzip-Folder");
                    if (CheckDownloadFolder() && CheckUnzipFolder())
                    {
                        textBlockHyperlink1.Visibility = Visibility.Visible;
                        textBlockHyperlink2.Visibility = Visibility.Visible;
                        button.IsEnabled = true;
                    }
                    break;
                case OperatingMode.SmartUpdate:
                    button.Content = "_Update";
                    hyperlink1.Inlines.Add("Update-Folder");
                    break;
            }

            WpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            WpfHelper.DisableHyperlinkHoverEffect(hyperlink1);
            WpfHelper.DisableHyperlinkHoverEffect(hyperlink2);

            textBlockProgressBar.Visibility = Visibility.Visible;
            progressBar.Value = 75;
            progressBar.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Visible;
        }

        private void CreateFolders()
        {

            if (!Directory.Exists(config.DownloadFolder))
            {
                if (WpfHelper.AskQuestion("The configured download folder not exists. Please make sure the folder exists."))
                {

                }

                
            }
        }





        private bool CheckDownloadFolder()
        {
            if (!Directory.Exists(config.DownloadFolder))
            {
                WpfHelper.ShowError("The configured download folder not exists. Please make sure the folder exists.");

                return false;
            }

            return true;
        }

        private bool CheckUnzipFolder()
        {
            if (!Directory.Exists(config.UnzipFolder))
            {
                WpfHelper.ShowError("The configured unzip folder not exists. Please make sure the folder exists.");

                return false;
            }

            return true;
        }

        private async Task ConfigureWebView()
        {
            await webView.EnsureCoreWebView2Async();

            webView.IsEnabled = false;
        }
    }
}
