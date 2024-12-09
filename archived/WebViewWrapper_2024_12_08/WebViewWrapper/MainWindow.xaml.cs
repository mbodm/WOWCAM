using System.ComponentModel;
using System.IO;
using System.Windows;
using static System.Net.WebRequestMethods;

namespace WebViewWrapper
{
    public partial class MainWindow : Window
    {
        private readonly WebViewDownloadHelper helper = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await WebView.EnsureCoreWebView2Async();
            await helper.CreateEnvironmentAsync();
            
            helper.Initialize(WebView.CoreWebView2);
            helper.DownloadFilesAsyncProgressChanged += Wrapper_DownloadFilesAsyncProgressChanged;
            helper.DownloadFilesAsyncCompleted += Wrapper_DownloadFilesAsyncCompleted;

            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            string[] urls =
            [
                "https://www.curseforge.com/wow/addons/deadly-boss-mods/download",
                "https://www.curseforge.com/wow/addons/details/download",
                "https://www.curseforge.com/wow/addons/groupfinderflags/download",
                "https://www.curseforge.com/wow/addons/raiderio/download",
                "https://www.curseforge.com/wow/addons/tomtom/download",
                "https://www.curseforge.com/wow/addons/weakauras-2/download"
            ];

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestDownloads");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            helper.DownloadFilesAsync(urls, folder);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            helper.CancelDownloadAddonsAsync();
        }

        private void Wrapper_DownloadFilesAsyncProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void Wrapper_DownloadFilesAsyncCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            helper.DownloadFilesAsyncProgressChanged -= Wrapper_DownloadFilesAsyncProgressChanged;
            helper.DownloadFilesAsyncCompleted -= Wrapper_DownloadFilesAsyncCompleted;

            if (e.Cancelled)
            {
                MessageBox.Show("Downloads cancelled by user");
            }
            else
            {
                MessageBox.Show("Downloads finished");
            }
        }
    }
}
