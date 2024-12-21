using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FinalPrototyping.WebView;
using WOWCAM.Core;
using WOWCAM.Helper;
using WOWCAM.WebView;

namespace FinalPrototyping
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger = new DefaultLogger();

        private readonly IWebViewProvider webViewProvider;
        private readonly IWebViewConfigurator webViewConfigurator;
        private readonly ICurseScraper curseScraper;

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

            //webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
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
                    IEnumerable<string> addonUrls = [
                        "https://www.curseforge.com/wow/addons/deadly-boss-mods",
                        "https://www.curseforge.com/wow/addons/details",
                        "https://www.curseforge.com/wow/addons/groupfinderflags",
                        "https://www.curseforge.com/wow/addons/raiderio",
                        "https://www.curseforge.com/wow/addons/tomtom",
                        "https://www.curseforge.com/wow/addons/weakauras-2"
                    ];

                    progressBar.Value = 0;
                    progressBar.Maximum = addonUrls.Count();




                    /*
                    List<string> downloadUrls = [];
                    foreach (var addonUrl in addonUrls)
                    {
                        var addonName = CurseHelper.GetAddonSlugNameFromAddonPageUrl(addonUrl);
                        textBlock.Text = $"Fetch {addonName}";

                        var downloadUrl = await curseScraper.GetAddonDownloadUrlAsync(addonUrl);
                        downloadUrls.Add(downloadUrl);

                        progressBar.Value++;
                    }
                    */

                    await GetDownloadUrlsAsync(addonUrls, new Progress<string>(p =>
                    {

                    })),





                    //var downloadUrls = await curseScraper.GetAddonDownloadUrlsAsync(addonUrls);
                    
                    sw.Stop();
                    MessageBox.Show($"Fetch finished. Time (in ms): {sw.ElapsedMilliseconds}");
                    
                    sw.Restart();
                    progressBar.Value = 0;
                    textBlock.Text = "Download...";

                    /*
                    var downloader = new DefaultWebViewDownloaderEAP(logger, webViewProvider);
                    
                    downloader.DownloadCompleted += (s, e) =>
                    {
                        sw.Stop();
                        MessageBox.Show($"Download finished. Time (in ms): {sw.ElapsedMilliseconds}");
                    };

                    downloader.DownloadProgressChanged += (s, e) =>
                    {
                        progressBar.Value++;
                    };
                    
                    downloader.DownloadAsync(downloadUrls, webViewConfigurator.GetDownloadFolder());
                    */

                    MessageBox.Show(string.Join(", ", downloadUrls));

                    return;

                    var downloader = new DefaultWebViewDownloader(logger, webViewProvider);

                    var progress = new Progress<string>(p =>
                    {
                        progressBar.Value++;
                    });

                    await downloader.DownloadFilesAsync(downloadUrls, progress);

                    sw.Stop();
                    MessageBox.Show($"Download finished. Time (in ms): {sw.ElapsedMilliseconds}");
                }
            }
        }

        private async Task GetDownloadUrlsAsync(IEnumerable<string> addonUrls, IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            var tasks = addonUrls.Select(addonUrl => curseScraper.GetAddonDownloadUrlAsync(addonUrl, cancellationToken));
            
            await Task.WhenAll(tasks);
        }
    }
}
