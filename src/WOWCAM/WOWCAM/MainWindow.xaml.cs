using System.Windows;
using WOWCAM.Core.Parts.Modules;
using WOWCAM.Helper.Parts.Application;
using WOWCAM.Logging;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly ISettingsModule settingsModule;
        private readonly ISystemModule systemModule;
        private readonly IUpdateModule updateModule;
        private readonly IAddonsModule addonsModule;

        public MainWindow(ILogger logger, ISettingsModule settingsModule, ISystemModule systemModule, IUpdateModule updateModule, IAddonsModule addonsModule)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settingsModule = settingsModule ?? throw new ArgumentNullException(nameof(settingsModule));
            this.systemModule = systemModule ?? throw new ArgumentNullException(nameof(systemModule));
            this.updateModule = updateModule ?? throw new ArgumentNullException(nameof(updateModule));
            this.addonsModule = addonsModule ?? throw new ArgumentNullException(nameof(addonsModule));

            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"WOWCAM {AppHelper.GetApplicationVersion()}";

            textBlockConfigFolder.Visibility = Visibility.Hidden;
            textBlockCheckUpdates.Visibility = Visibility.Hidden;

            SetProgress(false, string.Empty, 0, 100);
            SetControls(false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // Register a hook which reacts to a specific custom window message

            base.OnSourceInitialized(e);

            SingleInstance.RegisterHook(this);
        }
    }
}
