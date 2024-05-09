using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly IProcessHelper processHelper;
        private readonly IWebViewHelper webViewHelper;
        private readonly ICurseHelper curseHelper;
        private readonly IDownloadHelper downloadHelper;

        public MainWindow(
            ILogger logger,
            IConfig config,
            IConfigValidator configValidator,
            IProcessHelper processHelper,
            IWebViewHelper webViewHelper,
            ICurseHelper curseHelper,
            IDownloadHelper downloadHelper)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            this.webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
            this.downloadHelper = downloadHelper ?? throw new ArgumentNullException(nameof(downloadHelper));

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var jsonList = await webViewHelper.FetchJsonAsync(webView.CoreWebView2, config.AddonUrls);
            sw.Stop();
            var tuples = jsonList.Select(json =>
            {
                var jsonModel = curseHelper.SerializeAddonPageJson(json);
                var downloadUrl = curseHelper.BuildInitialDownloadUrl(jsonModel.ProjectId, jsonModel.FileId);

                return (downloadUrl, jsonModel);
            });
            WpfHelper.ShowInfo("Time: " + sw.ElapsedMilliseconds.ToString() + "ms for JSON");


            ulong sum = 0;


            var fuzz = tuples.Select(tuple => (double)tuple.jsonModel.FileSize).Sum();


            tuples.ToList().ForEach(tuple => sum += tuple.jsonModel.FileSize);



            long amende = 0;


            progressBar.Maximum = sum;
            var progress = new Progress<DownloadHelperProgressData>(p =>
            {
                progressBar.Value += p.ReceivedBytes;
                amende += p.ReceivedBytes;
            });
            sw.Restart();
            var tasks = tuples.Select(tuple => downloadHelper.DownloadAsync(tuple.downloadUrl, Path.Combine(config.TargetFolder, tuple.jsonModel.FileName), progress));
            await Task.WhenAll(tasks);
            sw.Stop();

            WpfHelper.ShowInfo("Time: " + sw.ElapsedMilliseconds.ToString() + "ms for download | amende = " + amende);
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
                if (!e.IsSuccess)
                {
                    logger.Log($"WebView2 initialization failed (the event's exception message was \"{e.InitializationException.Message}\").");

                    WpfHelper.ShowError("WebView2 initialization failed (see log file for details).");
                }
            };

            var environment = await webViewHelper.CreateEnvironmentAsync(config.TempFolder);

            await webView.EnsureCoreWebView2Async(environment);
        }
    }
}
