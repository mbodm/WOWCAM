using System.Windows;

namespace WOWCAM
{
    public partial class App : Application
    {
        public App()
        {
            MainWindow = new MainWindow(new Helper());
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow.Show();
        }
    }
}
