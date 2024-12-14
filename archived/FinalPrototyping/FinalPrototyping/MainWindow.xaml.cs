using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FinalPrototyping.Logger;
using FinalPrototyping.Scrap;
using FinalPrototyping.WebView;
using Microsoft.Web.WebView2.Core;

namespace FinalPrototyping
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger = new DefaultLogger();

        private readonly DefaultWebViewProvider webViewProvider;
        private readonly DefaultWebViewConfigurator webViewConfigurator;
        private readonly DefaultCurseScraper curseScraper;

        private readonly Stopwatch sw = new();

        public MainWindow()
        {
            InitializeComponent();

            webViewProvider = new DefaultWebViewProvider();
            webViewConfigurator = new DefaultWebViewConfigurator(webViewProvider);
            curseScraper = new DefaultCurseScraper(logger, webViewProvider);

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var env = await WebViewHelper.CreateEnvironmentAsync();
            await webView.EnsureCoreWebView2Async(env);

            webViewProvider.SetWebView(webView.CoreWebView2);
            webViewConfigurator.SetDownloadFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestDownloads"));
            webViewConfigurator.EnsureDownloadFolderExists();

            webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await webViewConfigurator.ClearDownloadFolderAsync();

            sw.Restart();

            if (sender is Button button)
            {
                if (button.Content.ToString() == "Start1")
                {
                    webView.CoreWebView2.Navigate("https://www.curseforge.com/wow/addons/raiderio/download");
                }

                if (button.Content.ToString() == "Start2")
                {
                    var downloadUrl = await curseScraper.GetAddonDownloadUrlAsync("https://www.curseforge.com/wow/addons/raiderio");
                    webView.CoreWebView2.Navigate(downloadUrl);
                }
            }
        }

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation op)
            {
                if (op.State == CoreWebView2DownloadState.Completed)
                {
                    sw.Stop();
                    MessageBox.Show($"Download finished. Time (in ms): {sw.ElapsedMilliseconds}");
                }
            }
        }
    }
}
