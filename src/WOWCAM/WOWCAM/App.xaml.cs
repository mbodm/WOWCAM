using System.IO;
using System.Net.Http;
using System.Windows;
using WOWCAM.Core.Parts.Logic.Addons;
using WOWCAM.Core.Parts.Logic.Config;
using WOWCAM.Core.Parts.Logic.System;
using WOWCAM.Core.Parts.Logic.Update;
using WOWCAM.Core.Parts.Modules;
using WOWCAM.Core.Parts.Update;
using WOWCAM.Helper.Parts.Application;
using WOWCAM.Logging;
using WOWCAM.WebView;

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

            var logFile = Path.Combine(AppHelper.GetApplicationExecutableFolder(), $"{AppHelper.GetApplicationName()}.log");
            var logger = new TextFileLogger(logFile);
            var reliableFileOperations = new DefaultReliableFileOperations();
            var configStorage = new XmlConfigStorage(logger, reliableFileOperations);
            var configReader = new XmlConfigReader(logger, configStorage);
            var configValidator = new XmlConfigValidator(logger);
            var settingsModule = new DefaultSettingsModule(logger, configStorage, configReader, configValidator);
            var systemModule = new DefaultSystemModule(logger);
            var updateManager = new DefaultUpdateManager(reliableFileOperations, httpClient);
            var updateModule = new DefaultUpdateModule(logger, settingsModule, updateManager);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var smartUpdateFeature = new DefaultSmartUpdateFeature(logger, reliableFileOperations);
            var singleAddonProcessor = new DefaultSingleAddonProcessor(webViewWrapper, smartUpdateFeature);
            var multiAddonProcessor = new DefaultMultiAddonProcessor(logger, singleAddonProcessor);
            var addonsModule = new DefaultAddonsModule(logger, webViewProvider, webViewWrapper, settingsModule, smartUpdateFeature, multiAddonProcessor, reliableFileOperations);

            MainWindow = new MainWindow(logger, settingsModule, systemModule, updateModule, addonsModule);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
