using System.Net.Http;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var logger = new DefaultLogger();
            var config = new DefaultConfig(logger);
            var fileSystemHelper = new DefaultFileSystemHelper();
            var curseHelper = new DefaultCurseHelper();
            var webViewHelper = new DefaultWebViewHelper(logger, curseHelper);

            MainWindow = new MainWindow(
                logger,
                config,
                new DefaultConfigValidator(logger, config, fileSystemHelper, curseHelper),
                webViewHelper,
                new DefaultProcessHelper(logger),
                new DefaultAddonProcessing(logger, webViewHelper, new DefaultDownloadHelper(new HttpClient()), new DefaultZipFileHelper(), fileSystemHelper));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
