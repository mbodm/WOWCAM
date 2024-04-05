using System.Windows;
using System.Windows.Documents;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private OperatingMode operatingMode = OperatingMode.DownloadAndUnzip;

        private readonly IWpfHelper wpfHelper;
        private readonly IConfigReader configReader;

        public MainWindow(IAppHelper appHelper, IWpfHelper wpfHelper, IConfigReader configReader)
        {
            if (appHelper is null)
            {
                throw new ArgumentNullException(nameof(appHelper));
            }

            this.wpfHelper = wpfHelper ?? throw new System.ArgumentNullException(nameof(wpfHelper));
            this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));

            InitializeComponent();

            // 16:10 format (1440x900 fits Curse site better than 1280x800)
            Width = 1440;
            Height = 900;
            MinWidth = 1440 / 2;
            MinHeight = 900 / 2;

            Title = $"WOWCAM {appHelper.GetApplicationVersion()}";

            HideControls();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            configReader.ReadConfig();
            operatingMode = configReader.OperatingMode;

            ConfigureControls();

            await ConfigureWebView();
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            wpfHelper.ShowInfo("Todo: Show config folder.");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                switch (operatingMode)
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
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (operatingMode)
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
            button.Visibility = Visibility.Hidden;
            textBlockHyperlink1.Visibility = Visibility.Hidden;
            textBlockHyperlink2.Visibility = Visibility.Hidden;
            hyperlink1.Inlines.Clear();
            hyperlink2.Inlines.Clear();

            switch (operatingMode)
            {
                case OperatingMode.DownloadOnly:
                    button.Content = "_Download";
                    hyperlink1.Inlines.Add("Download-Folder");
                    break;
                case OperatingMode.UnzipOnly:
                    button.Content = "_Unzip";
                    hyperlink1.Inlines.Add("Source-Folder");
                    hyperlink2.Inlines.Add("Dest-Folder");
                    textBlockHyperlink2.Visibility = Visibility.Visible;
                    break;
                case OperatingMode.DownloadAndUnzip:
                    button.Width = 125;
                    button.Content = "_Download & Unzip";
                    hyperlink1.Inlines.Add("Download-Folder");
                    hyperlink2.Inlines.Add("Unzip-Folder");
                    textBlockHyperlink2.Visibility = Visibility.Visible;
                    break;
                case OperatingMode.Update:
                    button.Content = "_Update";
                    hyperlink1.Inlines.Add("Update-Folder");
                    break;
            }

            textBlockHyperlink1.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Visible;

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
