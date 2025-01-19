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
            var settings = new DefaultSettings(logger, config);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, settings, httpClient);
            var smartUpdateFeature = new DefaultSmartUpdateFeature();
            var addonProcessing = new DefaultAddonProcessing(logger, webViewProvider, webViewWrapper, smartUpdateFeature);

            MainWindow = new MainWindow(logger, config, configValidator, settings, processStarter, updateManager, webViewProvider, addonProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
