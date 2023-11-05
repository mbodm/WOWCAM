namespace WOWCAM
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelWebView = new Panel();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            progressBar = new ProgressBar();
            buttonDownload = new Button();
            buttonUnzip = new Button();
            buttonDownloadAndUnzip = new Button();
            labelUnzipFolder = new Label();
            labelDownloadFolder = new Label();
            labelConfigFolder = new Label();
            labelStatus = new Label();
            panelWebView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // panelWebView
            // 
            panelWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelWebView.Controls.Add(webView);
            panelWebView.Location = new Point(12, 12);
            panelWebView.Name = "panelWebView";
            panelWebView.Size = new Size(920, 470);
            panelWebView.TabIndex = 0;
            panelWebView.Paint += PanelWebView_Paint;
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.Empty;
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 0);
            webView.Name = "webView";
            webView.Size = new Size(920, 470);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 519);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(572, 30);
            progressBar.TabIndex = 8;
            // 
            // buttonDownload
            // 
            buttonDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonDownload.Location = new Point(590, 519);
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(90, 30);
            buttonDownload.TabIndex = 4;
            buttonDownload.Text = "&Download";
            buttonDownload.UseVisualStyleBackColor = true;
            // 
            // buttonUnzip
            // 
            buttonUnzip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonUnzip.Location = new Point(686, 519);
            buttonUnzip.Name = "buttonUnzip";
            buttonUnzip.Size = new Size(90, 30);
            buttonUnzip.TabIndex = 5;
            buttonUnzip.Text = "&Unzip";
            buttonUnzip.UseVisualStyleBackColor = true;
            // 
            // buttonDownloadAndUnzip
            // 
            buttonDownloadAndUnzip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonDownloadAndUnzip.Location = new Point(782, 519);
            buttonDownloadAndUnzip.Name = "buttonDownloadAndUnzip";
            buttonDownloadAndUnzip.Size = new Size(150, 30);
            buttonDownloadAndUnzip.TabIndex = 6;
            buttonDownloadAndUnzip.Text = "Download and Unzip";
            buttonDownloadAndUnzip.UseVisualStyleBackColor = true;
            // 
            // labelUnzipFolder
            // 
            labelUnzipFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelUnzipFolder.AutoSize = true;
            labelUnzipFolder.Location = new Point(769, 485);
            labelUnzipFolder.Margin = new Padding(0, 0, 10, 0);
            labelUnzipFolder.Name = "labelUnzipFolder";
            labelUnzipFolder.Size = new Size(75, 15);
            labelUnzipFolder.TabIndex = 3;
            labelUnzipFolder.Text = "Unzip-Folder";
            // 
            // labelDownloadFolder
            // 
            labelDownloadFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelDownloadFolder.AutoSize = true;
            labelDownloadFolder.Location = new Point(660, 485);
            labelDownloadFolder.Margin = new Padding(0, 0, 10, 0);
            labelDownloadFolder.Name = "labelDownloadFolder";
            labelDownloadFolder.Size = new Size(99, 15);
            labelDownloadFolder.TabIndex = 2;
            labelDownloadFolder.Text = "Download-Folder";
            // 
            // labelConfigFolder
            // 
            labelConfigFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelConfigFolder.AutoSize = true;
            labelConfigFolder.Location = new Point(854, 485);
            labelConfigFolder.Margin = new Padding(0);
            labelConfigFolder.Name = "labelConfigFolder";
            labelConfigFolder.Size = new Size(81, 15);
            labelConfigFolder.TabIndex = 1;
            labelConfigFolder.Text = "Config-Folder";
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(9, 501);
            labelStatus.Margin = new Padding(0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(42, 15);
            labelStatus.TabIndex = 7;
            labelStatus.Text = "Status:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(944, 561);
            Controls.Add(labelStatus);
            Controls.Add(labelConfigFolder);
            Controls.Add(labelDownloadFolder);
            Controls.Add(labelUnzipFolder);
            Controls.Add(buttonDownloadAndUnzip);
            Controls.Add(buttonUnzip);
            Controls.Add(buttonDownload);
            Controls.Add(progressBar);
            Controls.Add(panelWebView);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm";
            Load += MainForm_Load;
            panelWebView.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelWebView;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private ProgressBar progressBar;
        private Button buttonDownload;
        private Button buttonUnzip;
        private Button buttonDownloadAndUnzip;
        private Label labelUnzipFolder;
        private Label labelDownloadFolder;
        private Label labelConfigFolder;
        private Label labelStatus;
    }
}