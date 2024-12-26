using System.Net.Http;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        private static readonly HttpClient httpClient = new();

        public App()
        {
            if (SingleInstance.AnotherInstanceIsAlreadyRunning)
            {
                SingleInstance.BroadcastMessage();
                Shutdown();
                return;
            }

            var logger = new DefaultLogger();
            var config = new DefaultConfig(logger);
            var configValidator = new DefaultConfigValidator(logger, config);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, config, httpClient);
            var addonProcessing = new DefaultAddonProcessing(logger, webViewWrapper);

            MainWindow = new MainWindow(logger, config, configValidator, processStarter, updateManager, webViewProvider, addonProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
