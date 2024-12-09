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
        private readonly IUpdateManager updateManager;
        private readonly IAddonProcessing addonProcessing;

        public MainWindow(
            ILogger logger,
            IConfig config,
            IConfigValidator configValidator,
            IWebViewWrapper webViewWrapper,
            IProcessStarter processStarter,
            IUpdateManager updateManager,
            IAddonProcessing addonProcessing)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
            this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
            this.updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            this.addonProcessing = addonProcessing ?? throw new ArgumentNullException(nameof(addonProcessing));

            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"WOWCAM {AppHelper.GetApplicationVersion()}";

            SetProgress(null, string.Empty, null, null);
            SetControls(false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // Register a hook which reacts to a specific custom window message

            base.OnSourceInitialized(e);

            SingleInstance.RegisterHook(this);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.ClearLog();

            try
            {
                if (!config.Exists) await config.CreateDefaultAsync();
                await config.LoadAsync();
                configValidator.Validate();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                Setlinks(true, 0);
                return;
            }

            await ConfigureWebViewAsync(webViewWrapper);

            SetControls(true);
            if (config.WebDebug) ShowWebView();

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
                SetProgress(true, null, null, null);

                var updateData = await updateManager.CheckForUpdateAsync();
                if (!updateData.UpdateAvailable)
                {
                    ShowInfo("You already have the latest WOWCAM version.");
                    return;
                }

                // Not sure how a MessageBox handles raw string literals (introduced in C# 11).
                // Therefore i decided to place the safe bet here and do it somewhat old-school.
                var question1 = string.Empty;
                question1 += $"A new WOWCAM version is available.{Environment.NewLine}";
                question1 += Environment.NewLine;
                question1 += $"This version: {updateData.InstalledVersion}{Environment.NewLine}";
                question1 += $"Latest version: {updateData.AvailableVersion}{Environment.NewLine}";
                question1 += Environment.NewLine;
                question1 += $"Download latest version now?{Environment.NewLine}";

                if (MessageBox.Show(question1, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                SetProgress(null, "Downloading application update", 0, null);
                await updateManager.DownloadUpdateAsync(updateData, new Progress<DownloadHelperProgress>(p =>
                {
                    var receivedMB = ((double)p.ReceivedBytes / 1024 / 1024).ToString("0.00", CultureInfo.InvariantCulture);
                    var totalMB = ((double)p.TotalBytes / 1024 / 1024).ToString("0.00", CultureInfo.InvariantCulture);

                    double? maximum = p.PreTransfer ? p.TotalBytes : null;
                    SetProgress(null, $"Downloading application update ({receivedMB} / {totalMB} MB)", p.ReceivedBytes, maximum);
                }));

                // Even with a typical semaphore-blocking-mechanism(*) it is impossible to prevent a WinForms/WPF
                // ProgressBar control from reaching its visual maximum AFTER the last async progress did happen.
                // The control is painted natively by the WinApi/OS itself. Therefore any event-based tricks will
                // not solve the problem. I just added a short async Wait() delay instead, to keep things simple.
                // (*)TAP concepts, when using IProgress<>, often need some semaphore-blocking-mechanism, because
                // a scheduler can still produce async progress, even when a Task.WhenAll() already has finished.
                await Task.Delay(1250);

                SetProgress(null, "Download finished", 1, 1);

                var question2 = $"Update successfully downloaded.{Environment.NewLine}{Environment.NewLine}Apply update now and restart application?";
                if (MessageBox.Show(question2, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                updateManager.ApplyUpdate();
                updateManager.RestartApplication();

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                SetControls(true);
                SetProgress(null, string.Empty, 0, 1);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            SetControls(false);
            SetProgress(true, string.Empty, 0, config.AddonUrls.Count() * 2);


            // Temp:
            var fff = config.AddonUrls.Where(url => !url.Contains("raiderio"));

            var sw = Stopwatch.StartNew();

            try
            {
                await addonProcessing.ProcessAddonsAsync(webView.CoreWebView2, fff, config.TempFolder, config.TargetFolder, new Progress<ModelAddonProcessingProgress>(p =>
                {
                    //if (p.State == EnumAddonProcessingState.StartingFetch) labelProgressBar.Content = $"Fetch {p.Addon}";
                    //if (p.State == EnumAddonProcessingState.StartingDownload) labelProgressBar.Content = $"Download {p.Addon}";
                    //if (p.State == EnumAddonProcessingState.StartingUnzip) labelProgressBar.Content = $"Unzip {p.Addon}";
                    //if (p.State == EnumAddonProcessingState.FinishedFetch || p.State == EnumAddonProcessingState.FinishedDownload || p.State == EnumAddonProcessingState.FinishedUnzip)
                    //{
                    //    progressBar.Value++;
                    //    if (progressBar.Value == progressBar.Maximum) labelProgressBar.Content = "Clean up";
                    //}

                    if (p.State == EnumAddonProcessingState.StartingFetch) labelProgressBar.Content = $"Fetch {p.Addon}";
                    if (p.State == EnumAddonProcessingState.StartingDownload) labelProgressBar.Content = $"Download {p.Addon}";
                    if (p.State == EnumAddonProcessingState.FinishedFetch || p.State == EnumAddonProcessingState.FinishedDownload)
                    {
                        progressBar.Value++;
                        if (progressBar.Value == progressBar.Maximum) labelProgressBar.Content = "Done.";
                    }
                }));

                // Even with a typical semaphore-blocking-mechanism* it is impossible to prevent a WinForms/WPF
                // ProgressBar control from reaching its maximum shortly after the last async progress happened.
                // The control is painted natively by the WinApi/OS itself. Therefore also no event-based tricks
                // will solve the problem. I just added a short async wait delay instead, to keep things simple.
                // *(TAP concepts, when using IProgress<>, often need some semaphore-blocking-mechanism, because
                // a scheduler can still produce async progress, even when Task.WhenAll() already has finished).
                await Task.Delay(1250);
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

            var seconds = Math.Round((double)(sw.ElapsedMilliseconds + 1250) / 1000);
            var rounded = Convert.ToUInt32(seconds);
            SetProgress(null, $"Successfully finished {config.AddonUrls.Count()} addons in {rounded} seconds", null, null);
        }
    }
}
