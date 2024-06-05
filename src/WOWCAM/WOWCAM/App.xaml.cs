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
            if (SingleInstanceManager.InstanceAlreadyRunning)
            {
                SingleInstanceManager.PostMessageToBringRunningInstanceToFront();
                MessageBox.Show("close myself now");
                Shutdown();
                return;
            }

            var appHelper = new DefaultAppHelper();
            var logger = new DefaultLogger();
            var config = new DefaultConfig(logger);
            var fileSystemHelper = new DefaultFileSystemHelper();
            var curseHelper = new DefaultCurseHelper();
            var configValidator = new DefaultConfigValidator(logger, config, fileSystemHelper, curseHelper);
            var webViewWrapper = new DefaultWebViewWrapper(logger, curseHelper);
            var processStarter = new DefaultProcessStarter(logger);
            var httpClient = new HttpClient();
            var gitHubHelper = new DefaultGitHubHelper(httpClient);
            var downloadHelper = new DefaultDownloadHelper(httpClient);
            var zipFileHelper = new DefaultZipFileHelper();
            var updateManager = new DefaultUpdateManager(logger, appHelper, gitHubHelper, config, fileSystemHelper, downloadHelper, zipFileHelper);
            var addonProcessing = new DefaultAddonProcessing(logger, curseHelper, webViewWrapper, downloadHelper, zipFileHelper, fileSystemHelper);

            MainWindow = new MainWindow(appHelper, logger, config, configValidator, webViewWrapper, processStarter, updateManager, addonProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
