using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WebViewWrapper.EAP.Many;
using WebViewWrapper.FromWOWCAM;
using WebViewWrapper.Logic;

namespace WebViewWrapper
{
    public partial class MainWindow : Window
    {
        private readonly IDownloadUrlsProvider downloadUrlsProvider;
        private readonly WebViewFileDownloaderM fileDownloader = new();
        private readonly Stopwatch sw = new Stopwatch();
        
        private IEnumerable<string> addonPageUrls = [];
        private IEnumerable<string> addonDownloadUrls = [];

        public MainWindow()
        {
            downloadUrlsProvider = new DefaultDownloadUrlsProvider(new DefaultCurseScraper());

            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var env = await fileDownloader.CreateEnvironmentAsync();
            await WebView.EnsureCoreWebView2Async(env);

            addonPageUrls =
            [
                "https://www.curseforge.com/wow/addons/deadly-boss-mods",
                "https://www.curseforge.com/wow/addons/details",
                "https://www.curseforge.com/wow/addons/groupfinderflags",
                //"https://www.curseforge.com/wow/addons/raiderio",
                "https://www.curseforge.com/wow/addons/tomtom",
                "https://www.curseforge.com/wow/addons/weakauras-2",
            ];

            progressBar.Minimum = 0;
            progressBar.Maximum = addonPageUrls.Count() * 2;
            progressBar.Value = 0;

            var progress1 = new Progress<DownloadUrlsProviderProgress>(downloadUrlsProviderProgress =>
            {
                progressBar.Value++;
                textBlock.Text = $"Scraping {downloadUrlsProviderProgress.AddonSlugName}";
            });

            sw.Restart();

            addonDownloadUrls = await downloadUrlsProvider.GetAddonDownloadUrls(WebView.CoreWebView2, addonPageUrls, progress1, CancellationToken.None);
                        
            //var seconds = Math.Round((double)(sw.ElapsedMilliseconds + 1250) / 1000);
            //var rounded = Convert.ToUInt32(seconds);
            //textBlock.Text = $"Successfully finished {addonPageUrls.Count()} addons in {rounded} seconds";
            
            textBlock.Text = "Downloading addons...";
            //progressBar.Minimum = 0;
            //progressBar.Maximum = addonPageUrls.Count();
            //progressBar.Value = 0;

            fileDownloader.Initialize(WebView.CoreWebView2);
            fileDownloader.DownloadFilesAsyncProgressChanged += Wrapper_DownloadFilesAsyncProgressChanged;
            fileDownloader.DownloadFilesAsyncCompleted += Wrapper_DownloadFilesAsyncCompleted;

            var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestDownloads");
            fileDownloader.DownloadFilesAsync(addonDownloadUrls, destFolder);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Todo
        }

        private void Wrapper_DownloadFilesAsyncProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar.Value++;
        }

        private void Wrapper_DownloadFilesAsyncCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            fileDownloader.DownloadFilesAsyncProgressChanged -= Wrapper_DownloadFilesAsyncProgressChanged;
            fileDownloader.DownloadFilesAsyncCompleted -= Wrapper_DownloadFilesAsyncCompleted;

            if (e.Cancelled)
            {
                MessageBox.Show("Downloads cancelled by user");
            }

            sw.Stop();
            var seconds = Math.Round((double)(sw.ElapsedMilliseconds) / 1000);
            var rounded = Convert.ToUInt32(seconds);
            textBlock.Text = $"Successfully finished {addonPageUrls.Count()} addons in {rounded} seconds";
        }
    }
}

/*
var html = """
    <!DOCTYPE html>
    <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Document</title>
        </head>
        <body>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 1</a></div>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 2</a></div>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 3</a></div>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 4</a></div>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 5</a></div>
            <div><a href="https://ash-speed.hetzner.com/100MB.bin">Download File 6</a></div>
            </br>
            <div><a href="https://www.curseforge.com/wow/addons/deadly-boss-mods/download">Addon URL 1</a></div>
            <div><a href="https://www.curseforge.com/wow/addons/details/download">Addon URL 2</a></div>
            <div><a href="https://www.curseforge.com/wow/addons/groupfinderflags/download">Addon-URL 3</a></div>
            <div><a href="https://www.curseforge.com/wow/addons/raiderio/download">Addon URL 4</a></div>
            <div><a href="https://www.curseforge.com/wow/addons/tomtom/download">Addon URL 5</a></div>
            <div><a href="https://www.curseforge.com/wow/addons/weakauras-2/download">Addon URL 6</a></div>
            </br>
            <div><a href="https://www.curseforge.com/api/v1/mods/3358/files/5961449/download">Download URL 1</a></div>
            <div><a href="https://www.curseforge.com/api/v1/mods/61284/files/5963026/download">Download URL 2</a></div>
            <div><a href="https://www.curseforge.com/api/v1/mods/103048/files/5628501/download">Download URL 3</a></div>
            <div><a href="https://www.curseforge.com/api/v1/mods/279257/files/5973353/download">Download URL 4</a></div>
            <div><a href="https://www.curseforge.com/api/v1/mods/18808/files/5940936/download">Download URL 5</a></div>
            <div><a href="https://www.curseforge.com/api/v1/mods/65387/files/5927090/download">Download URL 6</a></div>
        </body>
    </html>
    """;
*/
