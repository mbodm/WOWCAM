﻿using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public interface IWebViewHelper
    {
        Task<CoreWebView2Environment> CreateEnvironmentAsync(string tempFolder);
        Task<ModelAddonDownloadData> GetAddonDownloadUrlDataAsync(CoreWebView2 coreWebView, string addonUrl);
    }
}
