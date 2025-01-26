﻿using System.Net.Http;
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
            var configStorage = new XmlConfigStorage(logger);
            var configReader = new XmlConfigReader(logger, configStorage);
            var configValidator = new XmlConfigValidator(logger);
            var configModule = new ConfigModule(logger, configStorage, configReader, configValidator);
            var appSettings = new DefaultAppSettings(logger, configModule);
            var processStarter = new DefaultProcessStarter(logger);
            var updateManager = new DefaultUpdateManager(logger, appSettings, httpClient);
            var webViewProvider = new DefaultWebViewProvider();
            var webViewWrapper = new DefaultWebViewWrapper(logger, webViewProvider);
            var smartUpdateFeature = new DefaultSmartUpdateFeature(logger, appSettings);
            var addonProcessing = new DefaultAddonProcessing(webViewWrapper, smartUpdateFeature);
            var addonsProcessing = new DefaultAddonsProcessing(logger, addonProcessing, webViewProvider, webViewWrapper, smartUpdateFeature);

            MainWindow = new MainWindow(logger, configModule, appSettings, processStarter, updateManager, webViewProvider, webViewWrapper, addonsProcessing);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
