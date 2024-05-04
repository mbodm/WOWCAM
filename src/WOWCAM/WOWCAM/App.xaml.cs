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
            var configValidator = new XmlFileConfigValidator(logger, config, new DefaultCurseHelper());
            var processHelper = new DefaultProcessHelper(logger);
            var webViewHelper = new DefaultWebViewHelper(logger);

            MainWindow = new MainWindow(logger, config, configValidator, processHelper, webViewHelper);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
