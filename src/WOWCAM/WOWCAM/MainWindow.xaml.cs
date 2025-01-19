using System.Windows;
using WOWCAM.Core;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private readonly IConfigValidator configValidator;
        private readonly ISettings settings;
        private readonly IProcessStarter processStarter;
        private readonly IUpdateManager updateManager;
        private readonly IWebViewProvider webViewProvider;
        private readonly IAddonProcessing addonProcessing;

        public MainWindow(
            ILogger logger,
            IConfig config,
            IConfigValidator configValidator,
            ISettings settings,
            IProcessStarter processStarter,
            IUpdateManager updateManager,
            IWebViewProvider webViewProvider,
            IAddonProcessing addonProcessing)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
