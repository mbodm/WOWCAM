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
                if (!config.Exists)
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

        private void HyperlinkTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            WpfHelper.ShowInfo("Todo: Show target folder.");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WpfHelper.ShowInfo("Todo: Do stuff.");
        }

        private void InitControls()
        {
            textBlockConfigFolder.Visibility = Visibility.Hidden;
            textBlockTargetFolder.Visibility = Visibility.Hidden;
            textBlockProgressBar.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
            button.Visibility = Visibility.Hidden;
            button.IsEnabled = false;
        }

        private void ConfigureControls()
        {
            if (CheckFolders())
            {
                textBlockHyperlink1.Visibility = Visibility.Visible;
                textBlockHyperlink2.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }

            WpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
            WpfHelper.DisableHyperlinkHoverEffect(hyperlinkTargetFolder);

            textBlockProgressBar.Visibility = Visibility.Visible;
            progressBar.Value = 75;
            progressBar.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Visible;
        }
        
        private bool CheckFolders()
        {
            // I decided to NOT create the folder by code here since the default config contains assumptions about WoW folder in %PROGRAMFILES(X86)%

            if (!Directory.Exists(config.TargetFolder))
            {
                WpfHelper.ShowError("The configured target folder not exists. Please make sure the folder exists.");

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
