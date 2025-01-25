using System.Windows;
using WOWCAM.Core.Parts.Addons;
using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Settings;
using WOWCAM.Core.Parts.Tools;
using WOWCAM.Core.Parts.Update;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IAppSettings appSettings;
        private readonly IProcessStarter processStarter;
        private readonly IUpdateManager updateManager;
        private readonly IWebViewProvider webViewProvider;
        private readonly IAddonsProcessing addonProcessing;

        public MainWindow(
            ILogger logger,
            IConfig config,
            IAppSettings appSettings,
            IProcessStarter processStarter,
            IUpdateManager updateManager,
            IWebViewProvider webViewProvider,
            IAddonsProcessing addonProcessing)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
            this.updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            this.webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
            this.addonProcessing = addonProcessing ?? throw new ArgumentNullException(nameof(addonProcessing));

            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"WOWCAM {AppHelper.GetApplicationVersion()}";

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
