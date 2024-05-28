using System.Net.Http;
using System.Windows;
using WOWCAM.Core;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var appHelper = new DefaultAppHelper();
            var logger = new DefaultLogger();
            var config = new DefaultConfig(logger);

            var fileSystemHelper = new DefaultFileSystemHelper();
            var curseHelper = new DefaultCurseHelper();
            var configValidator = new DefaultConfigValidator(logger, config, fileSystemHelper, curseHelper);

            var webViewWrapper = new DefaultWebViewWrapper(logger, curseHelper);
            var processStarter = new DefaultProcessStarter(logger);

            var httpClient = new HttpClient();
            var downloadHelper = new DefaultDownloadHelper(httpClient);
            var zipFileHelper = new DefaultZipFileHelper();
            var addonProcessing = new DefaultAddonProcessing(logger, curseHelper, webViewWrapper, downloadHelper, zipFileHelper, fileSystemHelper);

            var gitHubHelper = new DefaultGitHubHelper(httpClient);
            var updateManager = new DefaultUpdateManager(logger, appHelper, fileSystemHelper, gitHubHelper, config, downloadHelper, zipFileHelper);

            MainWindow = new MainWindow(appHelper, logger, config, configValidator, webViewWrapper, processStarter, addonProcessing, updateManager);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
