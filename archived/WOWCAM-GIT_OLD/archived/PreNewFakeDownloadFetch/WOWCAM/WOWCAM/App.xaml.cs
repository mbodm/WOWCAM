using System.Windows;
using WOWCAM.Core;
using WOWCAM.WebView;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var logger = new FileLogger();
            var config = new XmlFileConfig(logger);
            var curseHelper = new DefaultCurseHelper();
            var configValidator = new XmlFileConfigValidator(logger, config, curseHelper);
            var processHelper = new DefaultProcessHelper(logger);
            var webViewHelper = new DefaultWebViewHelper(logger, curseHelper);

            MainWindow = new MainWindow(logger, config, configValidator, processHelper, webViewHelper, curseHelper);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
