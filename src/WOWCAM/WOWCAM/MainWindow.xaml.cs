using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly IAppHelper appHelper;
        private readonly IWpfHelper wpfHelper;

        private OperatingMode mode = OperatingMode.DownloadAndUnzip;

        public MainWindow(IAppHelper appHelper, IWpfHelper wpfHelper)
        {
            this.appHelper = appHelper ?? throw new System.ArgumentNullException(nameof(appHelper));
            this.wpfHelper = wpfHelper ?? throw new System.ArgumentNullException(nameof(wpfHelper));

            InitializeComponent();

            // 16:10 format (1440x900 fits Curse site better than 1280x800)
            Width = 1440;
            Height = 900;
            MinWidth = 1440 / 2;
            MinHeight = 900 / 2;

            Title = $"WOWCAM {appHelper.GetApplicationVersion()}";

            HideControls();
        }
    }
}
