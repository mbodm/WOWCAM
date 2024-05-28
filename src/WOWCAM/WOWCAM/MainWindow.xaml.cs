using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using WOWCAM.Core;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly IWebViewWrapper webViewWrapper;
        private readonly IProcessStarter processStarter;
        private readonly IAddonProcessing addonProcessing;
        private readonly IUpdateManager updateManager;

        public MainWindow(
            IAppHelper appHelper,
            ILogger logger,
            IConfig config,
            IConfigValidator configValidator,
            IWebViewWrapper webViewWrapper,
            IProcessStarter processStarter,
            IAddonProcessing addonProcessing,
            IUpdateManager updateManager)
        {
            ArgumentNullException.ThrowIfNull(appHelper);
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
            this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
            this.addonProcessing = addonProcessing ?? throw new ArgumentNullException(nameof(addonProcessing));
            this.updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));

            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"WOWCAM {appHelper.GetApplicationVersion()}";

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
                ShowError(ex.Message);
                Setlinks(true, 0);
                return;
            }

            // I decided to NOT create the folders by code here since the default config makes various assumptions i.e. about WoW folder in %PROGRAMFILES(X86)%

            if (!Directory.Exists(config.TempFolder))
            {
                ShowError("The configured temp folder not exists. Please make sure the folder exists.");
                return;
            }

            if (!Directory.Exists(config.TargetFolder))
            {
                ShowError("The configured target folder not exists. Please make sure the folder exists.");
                return;
            }

            await ConfigureWebViewAsync();

            if (config.WebDebug)
            {
                ShowWebView();
            }

            SetControls(true);

            button.TabIndex = 0;
            button.Focus();
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                processStarter.OpenFolderInExplorer(Path.GetDirectoryName(config.Storage) ?? string.Empty);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void HyperlinkCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetControls(false);
                SetProgress(true, true, "Check for updates");

                var updateData = await updateManager.CheckForUpdateAsync();
                if (!updateData.UpdateAvailable)
                {
                    ShowInfo("You already have the latest version.");
                    return;
                }

                var text = "A new version is available. Download and install now?";
                if (MessageBox.Show(text, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    return;
                }

                labelProgressBar.Content = "Downloading application update";
                await updateManager.DownloadUpdateAsync(updateData, new Progress<ModelDownloadHelperProgress>(p =>
                {
                    if (p.IsPreDownloadSizeDetermination) progressBar.Maximum = p.TotalBytes;

                    var totalMB = ((double)p.TotalBytes / 1024 / 1024).ToString("0.00", CultureInfo.InvariantCulture);
                    var receivedMB = ((double)p.ReceivedBytes / 1024 / 1024).ToString("0.00", CultureInfo.InvariantCulture);

                    labelProgressBar.Content = $"Downloading application update ({receivedMB} / {totalMB} MB)";
                    progressBar.Value = p.ReceivedBytes;
                }));

                // Even with a typical semaphore-blocking-mechanism* it is impossible to prevent a WinForms/WPF
                // ProgressBar control from reaching its maximum shortly after the last async progress happened.
                // The control is painted natively by the WinApi/OS itself. Therefore also no event-based tricks
                // will solve the problem. I just added a short async wait delay instead, to keep things simple.
                // *(TAP concepts, when using IProgress<>, often need some semaphore-blocking-mechanism, because
                // a scheduler can still produce async progress, even when Task.WhenAll() already has finished).
                await Task.Delay(1250);

                ShowInfo("Application will restart now and apply update.");
                var applied = await updateManager.ApplyUpdateAsync();
                if (!applied)
                {
                    ShowInfo("Update cancelled by user.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                SetControls(true);
                SetProgress(true, true);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            SetControls(false);
            SetProgress(true, true);
            progressBar.Maximum = config.AddonUrls.Count() * 3;

            var progress = new Progress<ModelAddonProcessingProgress>(p =>
            {
                if (p.State == EnumAddonProcessingState.StartingFetch) labelProgressBar.Content = $"Fetch {p.Addon}";
                if (p.State == EnumAddonProcessingState.StartingDownload) labelProgressBar.Content = $"Download {p.Addon}";
                if (p.State == EnumAddonProcessingState.StartingUnzip) labelProgressBar.Content = $"Unzip {p.Addon}";
                if (p.State == EnumAddonProcessingState.FinishedFetch || p.State == EnumAddonProcessingState.FinishedDownload || p.State == EnumAddonProcessingState.FinishedUnzip)
                {
                    progressBar.Value++;
                    if (progressBar.Value == progressBar.Maximum) labelProgressBar.Content = "Clean up";
                }
            });

            var sw = Stopwatch.StartNew();

            try
            {
                await addonProcessing.ProcessAddonsAsync(webView.CoreWebView2, config.AddonUrls, config.TempFolder, config.TargetFolder, progress);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return;
            }
            finally
            {
                SetControls(true);
            }

            sw.Stop();

            // Even with a typical semaphore-blocking-mechanism* it is impossible to prevent a WinForms/WPF
            // ProgressBar control from reaching its maximum shortly after the last async progress happened.
            // The control is painted natively by the WinApi/OS itself. Therefore also no event-based tricks
            // will solve the problem. I just added a short async wait delay instead, to keep things simple.
            // *(TAP concepts, when using IProgress<>, often need some semaphore-blocking-mechanism, because
            // a scheduler can still produce async progress, even when Task.WhenAll() already has finished).
            await Task.Delay(1250);

            var seconds = Math.Round((double)(sw.ElapsedMilliseconds + 1250) / 1000);
            var rounded = Convert.ToUInt32(seconds);
            labelProgressBar.Content = $"Successfully finished {config.AddonUrls.Count()} addons in {rounded} seconds";
        }
    }
}
