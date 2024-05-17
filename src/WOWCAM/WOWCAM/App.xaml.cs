using System.Net.Http;
using System.Windows;
using WOWCAM.Core;
using WOWCAM.Helpers;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var logger = new DefaultLogger();
            var appHelper = new DefaultAppHelper();
            var config = new DefaultConfig(logger);
            var fileSystemHelper = new DefaultFileSystemHelper();
            var curseHelper = new DefaultCurseHelper();
            var webViewHelper = new DefaultWebViewHelper(logger, curseHelper);
            var httpClient = new HttpClient();
            var downloadHelper = new DefaultDownloadHelper(httpClient);

            MainWindow = new MainWindow(
                logger,
                appHelper,
                config,
                new DefaultConfigValidator(logger, config, fileSystemHelper, curseHelper),
                webViewHelper,
                new DefaultProcessHelper(logger),
                new DefaultAddonProcessing(logger, curseHelper, webViewHelper, downloadHelper, new DefaultZipFileHelper(), fileSystemHelper),
                new DefaultUpdateManager(logger, appHelper, new DefaultGitHubHelper(httpClient), fileSystemHelper, downloadHelper));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
