using WOWCAM.Core;
using WOWCAM.WebView;

namespace WOWCAM
{
    public partial class MainForm : Form
    {
        private readonly IWebViewHelper webViewHelper;
        private readonly ILogger logger;
        private readonly IConfigReader configReader;
        private readonly IPlatformHelper platformHelper;

        public MainForm(IWebViewHelper webViewHelper, ILogger logger, IConfigReader configReader, IPlatformHelper platformHelper)
        {
            this.webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            this.platformHelper = platformHelper ?? throw new ArgumentNullException(nameof(platformHelper));

            InitializeComponent();

            Text = $"WOWCAD {GetVersion()}";
            MinimumSize = Size;
            SetPanelColor();
            Enabled = false;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            InitializeLogging();

            await InitializeWebViewAsync();

            //LoadConfig();

            ConfigureLabels();


            SetWebViewZoomFactor();
            webViewHelper.ShowStartPage();

            Enabled = true;
        }

        private void PanelWebView_Paint(object sender, PaintEventArgs e)
        {
            var realProgressBarBorderColor = Color.FromArgb(255, 188, 188, 188);

            ControlPaint.DrawBorder(e.Graphics, panelWebView.ClientRectangle, realProgressBarBorderColor, ButtonBorderStyle.Solid);
        }

        private static string GetVersion()
        {
            // Seems to be the most simple way to get the product version (semantic versioning) for .NET5/6 onwards.
            // Application.ProductVersion.ToString() is the counterpart of the "Version" entry in the .csproj file.

            return Application.ProductVersion.ToString();
        }

        private void SetPanelColor()
        {
            var realProgressBarBackColor = Color.FromArgb(255, 230, 230, 230);

            panelWebView.BackColor = realProgressBarBackColor;
        }

        private async Task InitializeWebViewAsync()
        {
            // The Microsoft WebView2 docs say: The CoreWebView2InitializationCompleted event is fired even before
            // the EnsureCoreWebView2Async() method ends. Therefore just awaiting that method is all we need here.

            var environment = await webViewHelper.CreateEnvironmentAsync();
            await webView.EnsureCoreWebView2Async(environment);
            webViewHelper.Initialize(webView.CoreWebView2);
        }

        private void InitializeLogging()
        {
            var logFile = Path.GetFullPath(logger.Storage); // Seems OK to me (since the BL/Program.cs knows the concrete implementation type anyway)

            if (!string.IsNullOrEmpty(logFile))
            {
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
            }
        }

        private void SetWebViewZoomFactor()
        {
            // 100% Windows-Setting --> DeviceDpi of  96
            // 125% Windows-Setting --> DeviceDpi of 120
            // 150% Windows-Setting --> DeviceDpi of 144

            if (DeviceDpi == 96)
            {
                webView.ZoomFactor = 1.00;
            }
            else if (DeviceDpi == 120)
            {
                webView.ZoomFactor = 0.71;
            }
            else if (DeviceDpi == 144)
            {
                webView.ZoomFactor = 0.60;
            }
            else
            {
                webView.ZoomFactor = 1.00;
            }
        }

        private void LoadConfig()
        {
            try
            {
                configReader.ReadConfig();
                //configReader.ValidateConfig();
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                ShowError("Error while loading config file (see log for details).");

                Environment.Exit(1);
            }
        }

        private void ConfigureLabels()
        {
            labelConfigFolder.ForeColor = new LinkLabel().LinkColor;
            labelDownloadFolder.ForeColor = new LinkLabel().LinkColor;
            labelUnzipFolder.ForeColor = new LinkLabel().LinkColor;

            labelConfigFolder.Cursor = Cursors.Hand;
            labelDownloadFolder.Cursor = Cursors.Hand;
            labelUnzipFolder.Cursor = Cursors.Hand;

            var toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 1000,
                ReshowDelay = 500,
                ShowAlways = true
            };

            toolTip.SetToolTip(labelDownloadFolder, "Show the configured download folder in Explorer");
            toolTip.SetToolTip(labelUnzipFolder, "Show the configured unzip folder in Explorer");
            toolTip.SetToolTip(labelConfigFolder, "Show the config folder in Explorer");

            var configFolder = Path.GetDirectoryName(configReader.Storage); // Seems OK to me (since the BL/Program.cs knows the concrete implementation type anyway)

            if (!string.IsNullOrEmpty(configFolder))
            {
                labelConfigFolder.Click += (s, e) => platformHelper.OpenWindowsExplorer(configFolder);
            }

            if (!string.IsNullOrEmpty(configReader.DownloadFolder))
            {
                labelDownloadFolder.Click += (s, e) => platformHelper.OpenWindowsExplorer(configReader.DownloadFolder);
            }

            // Todo: Change to unzip folder here.

            if (!string.IsNullOrEmpty(configReader.DownloadFolder))
            {
                labelUnzipFolder.Click += (s, e) => platformHelper.OpenWindowsExplorer(configReader.DownloadFolder);
            }

            labelStatus.Text = $"Ready to download {configReader.AddonUrls.Count()} addons";
        }


        private static void ShowError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
