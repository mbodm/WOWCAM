using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            MainWindow = new MainWindow(new XmlFileConfig(new FileLogger(), new DefaultCurseHelper()));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
