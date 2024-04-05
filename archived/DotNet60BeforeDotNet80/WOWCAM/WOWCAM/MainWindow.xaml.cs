﻿using System;
using System.Windows;
using WOWCAM.Core;

namespace WOWCAM
{
    public partial class MainWindow : Window
    {
        private readonly IWpfHelper wpfHelper;
        private readonly IConfigReader configReader;

        public MainWindow(IAppHelper appHelper, IWpfHelper wpfHelper, IConfigReader configReader)
        {
            if (appHelper is null)
            {
                throw new ArgumentNullException(nameof(appHelper));
            }

            this.wpfHelper = wpfHelper ?? throw new System.ArgumentNullException(nameof(wpfHelper));
            this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));

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
