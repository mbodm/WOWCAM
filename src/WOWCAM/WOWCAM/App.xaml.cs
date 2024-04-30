using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var logger = new FileLogger();
            var config = new XmlFileConfig(logger);

            MainWindow = new MainWindow(
                config,
                new XmlFileConfigValidator(
                    logger,
                    config,
                    new DefaultCurseHelper()),
                new DefaultProcessHelper(logger));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
