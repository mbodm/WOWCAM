﻿using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core.Parts.WebView
{
    public interface IWebViewProvider
    {
        void SetWebView(CoreWebView2 coreWebView2);
        CoreWebView2 GetWebView();
    }
}
