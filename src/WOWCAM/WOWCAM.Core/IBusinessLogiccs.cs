﻿using Microsoft.Web.WebView2.Core;

namespace WOWCAM.Core
{
    public interface IBusinessLogic
    {
        public Task ProcessAddonsAsync(CoreWebView2 coreWebView, IEnumerable<string> addonUrls, string tempFolder, string targetFolder,
            IProgress<bool>? progress, CancellationToken cancellationToken = default);
    }
}
