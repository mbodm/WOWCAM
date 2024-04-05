using System.Windows.Documents;

namespace WOWCAM
{
    public interface IWpfHelper
    {
        void DisableHyperlinkHoverEffect(Hyperlink hyperlink);
        void ShowInfo(string message);
        void ShowError(string message);
    }
}
