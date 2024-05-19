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
            var configValidator = new DefaultConfigValidator(logger, config, fileSystemHelper, curseHelper);

            var webViewHelper = new DefaultWebViewHelper(logger, curseHelper);
            var processHelper = new DefaultProcessHelper(logger);

            var httpClient = new HttpClient();
            var downloadHelper = new DefaultDownloadHelper(httpClient);
            var zipFileHelper = new DefaultZipFileHelper();
            var addonProcessing = new DefaultAddonProcessing(logger, curseHelper, webViewHelper, downloadHelper, zipFileHelper, fileSystemHelper);

            var gitHubHelper = new DefaultGitHubHelper(httpClient);
            var updateManager = new DefaultUpdateManager(logger, config, appHelper, gitHubHelper, fileSystemHelper, downloadHelper, zipFileHelper);

            MainWindow = new MainWindow(logger, appHelper, config, configValidator, webViewHelper, processHelper, addonProcessing, updateManager);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
