using System.Windows;
using System.Windows.Documents;

namespace WOWCAM
{
    public sealed class DefaultWpfHelper : IWpfHelper
    {
        public void DisableHyperlinkHoverEffect(Hyperlink hyperlink)
        {
            // By default a Hyperlink has a hover effect: The foreground color is changed on mouse hover.
            // Since i don´t want that behaviour and since Hyperlink is somewhat "special" in WPF and a
            // bit painful to style, i use a little trick here: I just set the Foreground property. This
            // prevents the Hyperlink from using the default hover color (red). Result: Effect disabled.

            hyperlink.Foreground = hyperlink.Foreground;
        }

        public void ShowInfo(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
