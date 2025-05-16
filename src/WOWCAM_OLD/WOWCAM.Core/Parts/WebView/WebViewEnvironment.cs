using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core.Parts.WebView
{
    public sealed class WebViewEnvironment
    {
        public static Task<CoreWebView2Environment> CreateAsync(string userDataFolder)
        {
            // Note1:
            // The WebView2 user data folder (UDF) has to have write access and the UDF´s default location is the executable´s folder.
            // Therefore some other folder (with write permissions guaranteed) should be specified here, used as UDF for the WebView2.
            // The UDF could reside in %TEMP% folder, in example, which would match the natural temporary characteristics the UDF has.

            // Note2:
            // The application, when started or closed, should NEVER try to delete given folder. On purpose! Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there). But
            // in my opinion this is totally fine, since it is a user´s temp folder and the UDF will be reused next time again anyway.

            return CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());
        }
    }
}
