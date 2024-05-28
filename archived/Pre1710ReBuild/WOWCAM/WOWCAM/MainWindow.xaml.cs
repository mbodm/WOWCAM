using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WOWCAM.Core;
using WOWCAM.Helpers;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly IWebViewHelper webViewHelper;
        private readonly IProcessHelper processHelper;
        private readonly IAddonProcessing addonProcessing;
        private readonly IUpdateManager updateManager;

        public MainWindow(
            ILogger logger,
            IAppHelper appHelper,
            IConfig config,
            IConfigValidator configValidator,
            IWebViewHelper webViewHelper,
            IProcessHelper processHelper,
            IAddonProcessing addonProcessing,
            IUpdateManager updateManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(appHelper);
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
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
                WpfHelper.ShowError(ex.Message);
                EnableConfigFolderHyperlink();
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

            button.MouseRightButtonUp += Button_MouseRightButtonUp;
            button.TabIndex = 0;
            button.Focus();
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

        private async void HyperlinkCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                labelProgressBar.Content = "Check for updates";
                progressBar.Maximum = 1;
                progressBar.Value = 0;
                
                SetControls(false);
                labelProgressBar.IsEnabled = true;
                progressBar.IsEnabled = true;

                var updateData = await updateManager.CheckForUpdateAsync();
                if (updateData.UpdateAvailable)
                {
                    WpfHelper.ShowInfo("Todo: Update available. Show old and new version. Ask question: Download and install?");

                    updateManager.PrepareForDownload();

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

                    WpfHelper.ShowInfo("Todo: Ask question: Apply update and restart app?");

                    await updateManager.PrepareForUpdateAsync();

                    if (!updateManager.StartUpdateAppWithAdminRights())
                    {
                        WpfHelper.ShowInfo("Update cancelled by user.");
                        return;
                    }
                }
                else
                {
                    WpfHelper.ShowInfo("You already have the latest version.");
                }
            }
            catch (Exception ex)
            {
                WpfHelper.ShowError(ex.Message);
            }
            finally
            {
                SetControls(true);
                
                labelProgressBar.Content = string.Empty;
                progressBar.Maximum = 1;
                progressBar.Value = 0;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            labelProgressBar.Content = string.Empty;
            progressBar.Value = 0;
            progressBar.Maximum = config.AddonUrls.Count() * 3;
            button.IsEnabled = false;

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
                WpfHelper.ShowError(ex.Message);
                button.IsEnabled = true;
                return;
            }

            sw.Stop();

            // Even with a typical semaphore-blocking-mechanism* it is impossible to prevent a WinForms/WPF
            // ProgressBar control from reaching its maximum shortly after the last async progress happened.
            // The control is painted natively by the WinApi/OS itself. Therefore also no event-based tricks
            // will solve the problem. I just added a short async wait delay instead, to keep things simple.
            // *(TAP concepts, when using IProgress<>, often need some semaphore-blocking-mechanism, because
            // a scheduler can still produce async progress, even when Task.WhenAll() already has finished).

            await Task.Delay(1250);

            var seconds = (double)(sw.ElapsedMilliseconds + 1250) / 1000;
            var s = seconds.ToString("0.00", CultureInfo.InvariantCulture);
            labelProgressBar.Content = $"Successfully finished {config.AddonUrls.Count()} addons in {s} seconds";
            button.IsEnabled = true;
        }

        private void Button_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                // All sizes are based on 16:10 format relations (in example 1280x800)

                if (!webView.IsEnabled)
                {
                    Width = 1440;
                    Height = 900;
                    Left = (SystemParameters.PrimaryScreenWidth / 2) - (Width / 2);
                    Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);

                    webView.Width = double.NaN;
                    webView.Height = double.NaN;
                    border.Visibility = Visibility.Visible;
                    webView.IsEnabled = true;
                }
                else
                {
                    webView.IsEnabled = false;
                    border.Visibility = Visibility.Hidden;
                    webView.Width = 0;
                    webView.Height = 0;

                    Width = 512;
                    Height = 160;
                    Left = (SystemParameters.PrimaryScreenWidth / 2) - (Width / 2);
                    Top = (SystemParameters.PrimaryScreenHeight / 2) - (Height / 2);
                }
            }
        }

        private void SetControls(bool enabled)
        {
            textBlockConfigFolder.IsEnabled = enabled;
            textBlockCheckUpdates.IsEnabled = enabled;
            labelProgressBar.IsEnabled = enabled;
            progressBar.IsEnabled = enabled;
            button.IsEnabled = enabled;

            if (enabled)
            {
                WpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
                WpfHelper.DisableHyperlinkHoverEffect(hyperlinkCheckUpdates);
            }
        }

        private void EnableConfigFolderHyperlink()
        {
            textBlockConfigFolder.IsEnabled = true;
            WpfHelper.DisableHyperlinkHoverEffect(hyperlinkConfigFolder);
        }

        private async Task ConfigureWebViewAsync()
        {
            webView.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (!e.IsSuccess)
                {
                    logger.Log($"WebView2 initialization failed (the event's exception message was '{e.InitializationException.Message}').");
                    WpfHelper.ShowError("WebView2 initialization failed (see log file for details).");
                }
            };

            var environment = await webViewHelper.CreateEnvironmentAsync(config.TempFolder);
            await webView.EnsureCoreWebView2Async(environment);
        }
    }
}
