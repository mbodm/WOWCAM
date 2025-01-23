using System.Net.Http;
using System.Windows;
using WOWCAM.Core.Parts.Addons;
using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.Tools;
using WOWCAM.Core.Parts.Update;
using WOWCAM.Core.Parts.WebView;

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

            var logger = new TextFileLogger();
            var config = new XmlConfig(logger);
            var appSettings = new DefaultAppSettings(logger, config);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, appSettings, httpClient);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var smartUpdateFeature = new DefaultSmartUpdateFeature();
            var addonProcessing = new DefaultAddonProcessing(logger, webViewProvider, webViewWrapper, smartUpdateFeature);

            MainWindow = new MainWindow(logger, config, appSettings, processStarter, updateManager, webViewProvider, addonProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
