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
            var reliableFileOperations = new DefaultReliableFileOperations();
            var configStorage = new XmlConfigStorage(logger, reliableFileOperations);
            var configReader = new XmlConfigReader(logger, configStorage);
            var configValidator = new XmlConfigValidator(logger);
            var appSettings = new DefaultAppSettings(logger, configStorage, configReader, configValidator);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, appSettings, reliableFileOperations, httpClient);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var smartUpdateFeature = new DefaultSmartUpdateFeature(logger, appSettings, reliableFileOperations);
            var singleAddonProcessor = new DefaultSingleAddonProcessor(appSettings, webViewWrapper, smartUpdateFeature);
            var multiAddonProcessor = new DefaultMultiAddonProcessor(logger, appSettings, singleAddonProcessor);
            var addonsProcessing = new DefaultAddonsProcessing(logger, appSettings, webViewProvider, multiAddonProcessor, smartUpdateFeature, reliableFileOperations);

            MainWindow = new MainWindow(logger, appSettings, processStarter, updateManager, webViewProvider, webViewWrapper, addonsProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
