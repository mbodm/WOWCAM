using System.IO;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly IProcessHelper processHelper;

        public MainWindow(IConfig config, IConfigValidator configValidator, IProcessHelper processHelper)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));

            InitializeComponent();

            // 16:10 format (1440x900 fits Curse site better than 1280x800)
            // In XAML it's 480x300 (for a better XAML editor preview size)
            Width = 1440;
            Height = 900;
            MinWidth = Width / 2;
            MinHeight = Height / 2;

            Title = $"WOWCAM {AppHelper.GetApplicationVersion()}";

            SetControls(false);
        }

        #region Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!config.Exists)
                {
                    await config.CreateEmptyAsync();
                }

                await config.LoadAsync();

                configValidator.Validate();
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);

                return;
            }

            if (!Directory.Exists(config.TargetFolder))
            {
                // I decided to NOT create the folder by code here since the default config contains assumptions about WoW folder in %PROGRAMFILES(X86)%

                WpfHelper.ShowError("The configured target folder not exists. Please make sure the folder exists.");

                return;
            }

            await ConfigureWebView();
            
            SetControls(true);
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                processHelper.OpenFolderInExplorer(Path.GetDirectoryName(config.Storage) ?? string.Empty);
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);
            }
        }

        private void HyperlinkTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                processHelper.OpenFolderInExplorer(config.TargetFolder);
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WpfHelper.ShowInfo("Todo: Do stuff.");
        }

        #endregion

        #region Methods

        private void SetControls(bool enabled)
        {
            textBlockConfigFolder.IsEnabled = enabled;
            textBlockTargetFolder.IsEnabled = enabled;
            labelProgressBar.IsEnabled = enabled;
            progressBar.IsEnabled = enabled;
            button.IsEnabled = enabled;
            
            if (enabled)
            {
                WpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
                WpfHelper.DisableHyperlinkHoverEffect(hyperlinkTargetFolder);

                progressBar.Value = 75; // Todo: Remove after testing.
            }
        }

        private async Task ConfigureWebView()
        {
            await webView.EnsureCoreWebView2Async();

            webView.IsEnabled = false;
        }

        #endregion
    }
}
