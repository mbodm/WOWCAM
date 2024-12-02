using System.Windows;
using WOWCAM.Helper;

namespace WOWCAM.Update
{
    public partial class App : Application
    {
        public App()
        {
            MainWindow = new MainWindow(new DefaultAppHelper(), new ProcessHelper(), new FileSystemHelper());
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
