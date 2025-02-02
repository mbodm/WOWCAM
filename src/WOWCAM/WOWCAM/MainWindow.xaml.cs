using System.Windows;
using WOWCAM.Core.Parts.Addons;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.Modules;
using WOWCAM.Core.Parts.System;
using WOWCAM.Core.Parts.Update;
using WOWCAM.Core.Parts.WebView;
using WOWCAM.Helper;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly ILogger logger;
        private readonly IAppSettings configModule;
        private readonly IProcessStarter processStarter;
        private readonly IUpdateManager updateManager;
        private readonly IWebViewProvider webViewProvider;
        private readonly IWebViewWrapper webViewWrapper;
        private readonly ISingleAddonProcessor addonsProcessing;

        public MainWindow(
            ILogger logger,
            IAppSettings configModule,
            IProcessStarter processStarter,
            IUpdateManager updateManager,
            IWebViewProvider webViewProvider,
            IWebViewWrapper webViewWrapper,
            ISingleAddonProcessor addonsProcessing)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configModule = configModule ?? throw new ArgumentNullException(nameof(configModule));
            this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
            this.updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            this.webViewProvider = webViewProvider ?? throw new ArgumentNullException(nameof(webViewProvider));
            this.webViewWrapper = webViewWrapper ?? throw new ArgumentNullException(nameof(webViewWrapper));
            this.addonsProcessing = addonsProcessing ?? throw new ArgumentNullException(nameof(addonsProcessing));

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
