using System.Windows;

namespace WOWCAM.Update
{
    public partial class App : Application
    {
        public App()
        {
            MainWindow = new MainWindow();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
