using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            MainWindow = new MainWindow(new DefaultAppHelper(), new DefaultWpfHelper(), new XmlConfigReader(new DefaultCurseHelper()));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
