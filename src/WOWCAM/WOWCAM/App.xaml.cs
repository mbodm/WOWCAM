using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            var logger = new FileLogger();

            MainWindow = new MainWindow(new XmlFileConfig(logger, new DefaultCurseHelper()), new DefaultProcessHelper(logger));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
