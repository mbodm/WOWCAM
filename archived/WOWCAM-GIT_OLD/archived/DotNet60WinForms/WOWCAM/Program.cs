using WOWCAM.Core;
using WOWCAM.WebView;

namespace WOWCAM
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Dependency-Graph (using PoorMans-DI here, since such a small application needs no DI-Container, in my opinion)
            ICurseHelper curseHelper = new Version1CurseHelper();
            ILogger logger = new FileLogger();
            IWebViewHelper webViewHelper = new DefaultWebViewHelper(curseHelper, logger);
            IConfigReader configReader = new XmlConfigReader(curseHelper);
            IPlatformHelper platformHelper = new DefaultPlatformHelper();

            Application.Run(new MainForm(webViewHelper, logger, configReader, platformHelper));
        }
    }
}
