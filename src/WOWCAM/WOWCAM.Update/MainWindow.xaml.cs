using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using WOWCAM.Helpers;

namespace WOWCAM.Update
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            label.Content = string.Empty;
            progressBar.Maximum = 1;
            progressBar.Value = 0;

            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("mbodm");

            var progress = new Progress<ModelDownloadHelperProgress>(p =>
            {
                if (p.ReceivedBytes == 0)
                {
                    label.Content = $"0 / {p.TotalBytes}";
                    progressBar.Maximum = p.TotalBytes;
                }
                else
                {
                    label.Content = $"{p.ReceivedBytes} / {p.TotalBytes}";
                    progressBar.Value = p.ReceivedBytes;
                }
            });

            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Testo.zip");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var helper = new DefaultDownloadHelper(httpClient);
            await helper.DownloadFileAsync("https://go.microsoft.com/fwlink/?linkid=2088631", filePath, progress);

            MessageBox.Show("foobar");
        }
    }
}
