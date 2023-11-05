using System.Windows.Documents;

namespace WOWCAM
{
    public interface IHelper
    {
        string GetApplicationVersion();
        void DisableHyperlinkHoverEffect(Hyperlink hyperlink);
    }
}
