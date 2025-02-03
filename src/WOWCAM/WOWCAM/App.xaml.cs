using System.Net.Http;
using System.Windows;
using WOWCAM.Core.Parts.Addons;
using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.System;
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
            var configStorage = new XmlConfigStorage(logger);
            var configReader = new XmlConfigReader(logger, configStorage);
            var configValidator = new XmlConfigValidator(logger);
            var reliableFileOperations = new DefaultReliableFileOperations();
            var appSettings = new DefaultAppSettings(logger, configStorage, configReader, configValidator, reliableFileOperations);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, appSettings, httpClient);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var smartUpdateFeature = new DefaultSmartUpdateFeature(logger, appSettings);
            var addonProcessing = new DefaultSingleAddonProcessor(webViewWrapper, smartUpdateFeature);
            var addonsProcessing = new DefaultAddonsProcessing(logger, configModule, addonProcessing, webViewProvider, smartUpdateFeature);

            MainWindow = new MainWindow(logger, configModule, processStarter, updateManager, webViewProvider, webViewWrapper, addonsProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
