using System.Diagnostics.Metrics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using WOWCAM.Core;
using WOWCAM.WebView;
using static System.Net.WebRequestMethods;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly IProcessHelper processHelper;
        private readonly IWebViewHelper webViewHelper;

        public MainWindow(ILogger logger, IConfig config, IConfigValidator configValidator, IProcessHelper processHelper, IWebViewHelper webViewHelper)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            this.webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));

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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.ClearLog();

            try
            {
                if (!config.Exists)
                {
                    await config.CreateDefaultAsync();
                }

                await config.LoadAsync();

                configValidator.Validate();
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);

                return;
            }

            // I decided to NOT create the folders by code here since the default config makes various assumptions i.e. about WoW folder in %PROGRAMFILES(X86)%

            if (!Directory.Exists(config.TempFolder))
            {
                WpfHelper.ShowError("The configured temp folder not exists. Please make sure the folder exists.");
                return;
            }

            if (!Directory.Exists(config.TargetFolder))
            {
                WpfHelper.ShowError("The configured target folder not exists. Please make sure the folder exists.");
                return;
            }


            await ConfigureWebViewAsync();

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
            var startMessage = "Start addon download ================================================================================";
            var addonQueue = new Queue<string>(config.AddonUrls);
            progressBar.Maximum = addonQueue.Count;

            webViewHelper.DownloadCompleted += (s, e) =>
            {
                logger.Log(startMessage);

                //labelProgressBar.Content = counter.ToString();
                progressBar.Value++;

                if (addonQueue.Count > 0)
                {
                    webViewHelper.DownloadAsync(addonQueue.Dequeue());
                }
                else
                {
                    WpfHelper.ShowInfo("Alle feddig.");
                }
            };

            logger.Log(startMessage);

            webViewHelper.DownloadAsync(addonQueue.Dequeue());
        }

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
            }
        }

        private async Task ConfigureWebViewAsync()
        {
            webView.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (sender is WebView2 webView)
                {
                    if (e.IsSuccess)
                    {
                        webViewHelper.Initialize(webView.CoreWebView2, config.TargetFolder);
                    }
                    else
                    {
                        logger.Log($"WebView2 initialization failed (the event's exception message was \"{e.InitializationException.Message}\").");

                        WpfHelper.ShowError("WebView2 initialization failed (see log file for details).");
                    }
                }
            };

            var environment = await webViewHelper.CreateEnvironmentAsync(config.TempFolder);

            await webView.EnsureCoreWebView2Async(environment);
        }
    }
}
