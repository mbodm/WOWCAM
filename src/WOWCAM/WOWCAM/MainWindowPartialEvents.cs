using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.ClearLog();
            logger.Log("Application started and log file was cleared.");

            await updateManager.RemoveBakFileIfExistsAsync();
            await configModule.LoadAsync();
            await ConfigureWebViewAsync(configModule.AppSettings.WebViewEnvironmentFolder);
            webViewProvider.SetWebView(webView.CoreWebView2);

            SetControls(true);
            button.TabIndex = 0;
            button.Focus();
        }

        private void HyperlinkConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                processStarter.OpenFolderInExplorer(Path.GetDirectoryName(configModule.StorageInformation) ?? string.Empty);
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
                await updateManager.DownloadUpdateAsync(updateData, new Progress<DownloadProgress>(p =>
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

                await updateManager.ApplyUpdateAsync();
                updateManager.RestartApplication(2);
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

        private void ProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is ProgressBar && e.ChangedButton == MouseButton.Right && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (FindResource("keyContextMenu") is ContextMenu contextMenu)
                {
                    contextMenu.Items.Clear();

                    var itemProgramFolder = new MenuItem { Header = "Show program folder", Icon = new TextBlock { Text = "  1" } };
                    itemProgramFolder.Click += (s, e) => processStarter.OpenFolderInExplorer(AppHelper.GetApplicationExecutableFolder());
                    contextMenu.Items.Add(itemProgramFolder);
                    
                    var itemLogFile = new MenuItem { Header = "Show log file", Icon = new TextBlock { Text = "  2" } };
                    itemLogFile.Click += (s, e) => processStarter.ShowLogFileInNotepad();
                    contextMenu.Items.Add(itemLogFile);
                    
                    var itemAddonsFolder = new MenuItem { Header = "Show addons folder", Icon = new TextBlock { Text = "  3" } };
                    itemAddonsFolder.Click += (s, e) => processStarter.OpenFolderInExplorer(configModule.AppSettings.AddonTargetFolder);
                    contextMenu.Items.Add(itemAddonsFolder);

                    if (!webView.IsEnabled)
                    {
                        var itemWebView = new MenuItem { Header = "Activate Debug-Mode (WebView2)", Icon = new TextBlock { Text = "  4" } };

                        itemWebView.Click += (s, e) =>
                        {
                            var question = string.Empty;
                            question += $"Debug-Mode enables WebView2, with active dev tools.{Environment.NewLine}";
                            question += $"Don't click any web content while progress is running!{Environment.NewLine}{Environment.NewLine}";
                            question += $"Activate Debug-Mode?{Environment.NewLine}";
                            if (MessageBox.Show(question, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                ShowWebView();
                            }
                        };

                        contextMenu.Items.Add(itemWebView);
                    }

                    contextMenu.IsOpen = true;
                }

                e.Handled = true;
            }
        }

        private CancellationTokenSource? cts = null;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }
            
            if (button.Content.ToString() == "_Cancel")
            {
                if (cts != null)
                {
                    await cts.CancelAsync();
                }
            }
            else
            {
                button.Content = "_Cancel";
                cts = new CancellationTokenSource();

                SetControls(false);
                SetProgress(true, "Processing addons ...", 0, 100);
                button.IsEnabled = true;
                
                webViewWrapper.HideDownloadDialog = !webView.IsEnabled;
                
                var updatedAddons = 0u;
                var stopwatch = new Stopwatch();
                try
                {
                    stopwatch.Start();
                    updatedAddons = await addonsProcessing.ProcessAddonsAsync(
                        configModule.AppSettings.AddonUrls, configModule.AppSettings.AddonTargetFolder, configModule.AppSettings.WorkFolder,
                        new Progress<byte>(p => progressBar.Value = p), cts.Token);
                    stopwatch.Stop();

                    SetProgress(null, "Clean up ...", null, null);

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
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        SetProgress(null, "Cancelled by user", null, null);
                    }
                    else
                    {
                        SetProgress(null, "Error occurred", null, null);
                        ShowError(ex.Message);
                    }

                    return;
                }
                finally
                {
                    button.Content = "_Start";
                    SetControls(true);
                }

                var seconds = Math.Round((double)(stopwatch.ElapsedMilliseconds + 1250) / 1000);
                var rounded = Convert.ToUInt32(seconds);
                var addonOrAddons = PluralizeHelper.PluralizeWord("addon", () => updatedAddons != 1);
                var statusText = $"Successfully updated {updatedAddons} {addonOrAddons} in {rounded} seconds";
                
                SetProgress(null, statusText, null, null);
            }
        }
    }
}
