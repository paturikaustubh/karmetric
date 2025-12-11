using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ActivityMonitor.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async();

            // Set up virtual host mapping for local files
            // Maps http://app.local/ to c:\Practice\monitor-er\application\
            // Note: Folder path must be absolute and valid
            string appFolder = Path.Combine("c:\\Practice\\monitor-er", "ActivityMonitor.UI", "application");
            
            // Allow mixed content if needed (but we are mapping all to http://app.local)
            // But API is http://localhost:2369. Mixed content might be an issue (http vs http).
            // Actually, both are http, so no SSL mixed content issue.
            
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", 
                appFolder, 
                CoreWebView2HostResourceAccessKind.Allow
            );

            // Navigate to the index page via the virtual host
            webView.CoreWebView2.Navigate("http://app.local/pages/index.html");
        }
    }
}