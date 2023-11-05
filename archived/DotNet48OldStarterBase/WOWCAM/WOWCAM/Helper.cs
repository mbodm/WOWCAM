using System.Diagnostics;
using System.Reflection;
using System.Windows.Documents;

namespace WOWCAM
{
    public sealed class Helper : IHelper
    {
        public string GetApplicationVersion()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(executingAssembly.Location);

            return fileVersionInfo.ProductVersion;
        }

        public void DisableHyperlinkHoverEffect(Hyperlink hyperlink)
        {
            // By default a Hyperlink has a hover effect: The foreground color is changed on mouse hover.
            // Since i don´t want that behaviour and since Hyperlink is somewhat "special" in WPF and a
            // bit painful to style, i use a little trick here: I just set the Foreground property. This
            // prevents the Hyperlink from using the default hover color (red). Result: Effect disabled.

            hyperlink.Foreground = hyperlink.Foreground;
        }
    }
}
