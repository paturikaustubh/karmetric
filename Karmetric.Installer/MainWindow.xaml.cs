using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Reflection;
using Microsoft.Win32;
using System.Linq;

namespace Karmetric.Installer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // TxtVersion is set in XAML? No, let's keep the dynamic version set if it was there but I don't see it in the view.
            // Ah, line 18 in view was "CheckPrerequisites();".
            
            // Set default state explicitly (though XAML might have it)
            ChkRuntime.IsChecked = true;
            ChkRuntime.Content = "Install .NET 8 (ASP.NET Core)";

            ChkNode.IsChecked = true;
            ChkNode.Content = "Install Node.js";
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            GridWelcome.Visibility = Visibility.Collapsed;
            GridInstalling.Visibility = Visibility.Visible;

            // Capture UI State here (Main Thread)
            bool installRuntime = ChkRuntime.IsChecked == true;
            bool installNode = ChkNode.IsChecked == true;
            bool createDesktop = ChkDesktopShortcut.IsChecked == true;

            try
            {
                await Task.Run(() => PerformInstall(installRuntime, installNode, createDesktop));
                
                GridInstalling.Visibility = Visibility.Collapsed;
                GridComplete.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                GridInstalling.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Installation Failed: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GridWelcome.Visibility = Visibility.Visible;
            }
        }

        private void PerformInstall(bool installRuntime, bool installNode, bool createDesktop)
        {
            UpdateStatus("Initializing...");

            if (installRuntime)
            {
                UpdateStatus("Downloading .NET 8 (ASP.NET Core)...");
                // ASP.NET Core Runtime (Required for Worker Service with Web API)
                string runtimeUrl = "https://aka.ms/dotnet/8.0/aspnetcore-runtime-win-x64.exe";
                string installerPath = Path.Combine(Path.GetTempPath(), "dotnet_installer.exe");

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(runtimeUrl, installerPath);
                }

                UpdateStatus("Installing .NET Runtime...");
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/install /quiet /norestart", // Silent install
                    UseShellExecute = true,
                    Verb = "runas" // Request Admin
                });
                p.WaitForExit();
            }

            if (installNode)
            {
                UpdateStatus("Downloading Node.js...");
                // Node v20 LTS
                string nodeUrl = "https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi";
                string installerPath = Path.Combine(Path.GetTempPath(), "node_installer.msi");

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(nodeUrl, installerPath);
                }

                UpdateStatus("Installing Node.js...");
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{installerPath}\" /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas" // Request Admin
                });
                p.WaitForExit();
            }

            UpdateStatus("Extracting Installer Resources...");
            string tempFolder = Path.Combine(Path.GetTempPath(), "ActivityMonitor_Install_" + Guid.NewGuid().ToString().Substring(0, 8));
            Directory.CreateDirectory(tempFolder);

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                                              .FirstOrDefault(rn => rn.EndsWith("payload.zip"));

                if (string.IsNullOrEmpty(resourceName))
                {
                    // List available resources for debugging
                    var available = string.Join(", ", assembly.GetManifestResourceNames());
                    throw new Exception($"Installer Payload not found! Build issue. Available resources: {available}");
                }

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) throw new Exception("Installer Payload stream is null!");
                    
                    string zipPath = Path.Combine(tempFolder, "payload.zip");
                    using (var fileStream = File.Create(zipPath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    // Extract logic (Native .NET 4.5+)
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempFolder);
                }

                string repoRoot = tempFolder; // Simulating the root
                string bgSource = Path.Combine(repoRoot, "Background");
                // UI Source and App Source removed in Headless/Web architecture

                // Read Version from monitor.json in extracted payload
                string version = "1.0.0"; // Fallback
                try
                {
                    string jsonPath = Path.Combine(repoRoot, "monitor.json");
                    if (File.Exists(jsonPath))
                    {
                        var json = File.ReadAllText(jsonPath);
                        int idx = json.IndexOf("\"version\":");
                        if (idx != -1)
                        {
                            int start = json.IndexOf("\"", idx + 10) + 1;
                            int end = json.IndexOf("\"", start);
                            version = json.Substring(start, end - start);
                        }
                    }
                }
                catch {}

                UpdateStatus($"Verifying Version: {version}...");

                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Karmetric");

                UpdateStatus("Stopping existing processes...");

                // Attempt Graceful Session End via API
                try 
                {
                    UpdateStatus("Ending current session...");
                    using (var client = new System.Net.WebClient())
                    {
                        client.UploadString("http://localhost:2369/api/activity/end-session?reason=Installer+Update", "POST", "");
                    }
                    System.Threading.Thread.Sleep(1000); // Wait for DB write
                }
                catch 
                {
                    // Ignore if service not running or call fails
                }

                KillProcess("Karmetric.Background");
                KillProcess("Karmetric.UI"); // Restored for cleanup
                System.Threading.Thread.Sleep(2000);

                UpdateStatus("Copying Core Files...");
                CopyDirectory(bgSource, targetDir);
                // CopyDirectory(uiSource, targetDir); // Removed

                UpdateStatus("Configuring Settings...");
                string appSettingsPath = Path.Combine(targetDir, "appsettings.json");
                string jsonContent = $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning"",
      ""Microsoft.Hosting.Lifetime"": ""Information""
    }}
  }}
}}";
                File.WriteAllText(appSettingsPath, jsonContent);

                UpdateStatus("Cleaning existing startup entries...");
                string bgExe = Path.Combine(targetDir, "Karmetric.Background.exe");
                
                // Clean up all existing startup registrations to prevent duplicates
                CleanupExistingStartupEntries();
                CleanupLegacyUI(targetDir); // NEW: Remove old Folder/Shortcuts
                
                UpdateStatus("Registering Auto-Start...");
                // Add to Startup Folder only (not registry to avoid duplicates)
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                CreateShortcut("Karmetric Background", bgExe, startupFolder);
                
                UpdateStatus("Starting background service...");
                var psi = new ProcessStartInfo
                {
                    FileName = bgExe,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
                
                UpdateStatus("Creating Shortcuts...");
                
                // 1. Desktop Shortcut (Launcher)
                if (createDesktop)
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    CreateShortcut("Karmetric", bgExe, desktop, "--ui");
                }

                // 2. Start Menu Shortcuts (Launcher)
                string programsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string appStartMenuDir = Path.Combine(programsDir, "Karmetric");
                if (!Directory.Exists(appStartMenuDir)) Directory.CreateDirectory(appStartMenuDir);

                // Create Application Shortcut (Launcher)
                CreateShortcut("Karmetric", bgExe, appStartMenuDir, "--ui");
                
                // Copy Uninstaller files (Robust Search)
                // Search recursively in case user put it in a subfolder
                var uninstallerExe = Directory.GetFiles(repoRoot, "Karmetric.Uninstaller.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (uninstallerExe != null)
                {
                    string sourceDir = Path.GetDirectoryName(uninstallerExe);
                    // Copy detection: .exe, .dll, .json
                    var filesToCopy = Directory.GetFiles(sourceDir, "Karmetric.Uninstaller.*");
                    
                    foreach(var f in filesToCopy)
                    {
                        string fName = Path.GetFileName(f);
                        File.Copy(f, Path.Combine(targetDir, fName), true);
                    }

                    UpdateStatus("Registering Uninstaller...");
                    RegisterUninstaller(targetDir, version);
                }
                else 
                {
                    // Fallback or Log? 
                    // If not found, we can't register.
                    // Just proceed.
                }

                UpdateStatus("Cleanup...");
                try { Directory.Delete(tempFolder, true); } catch {}
                
                UpdateStatus("Done!");
                System.Threading.Thread.Sleep(500); // Visual pause
            }
            catch (Exception ex)
            {
                // Cleanup on error
                try { Directory.Delete(tempFolder, true); } catch {}
                throw ex;
            }
        }

        private void KillProcess(string name)
        {
            try 
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try 
                    { 
                        // Try graceful stop first to allow Session Checkout (DB update)
                        // 'taskkill' sends WM_CLOSE which works better for hidden message loops than CloseMainWindow
                        var stopPsi = new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/IM \"{name}.exe\"", // No /F force flag
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(stopPsi).WaitForExit();

                        // Wait for it to handle the signal and exit
                        if (!p.WaitForExit(5000))
                        {
                            // If still running after 5s, force kill
                            p.Kill();
                        }
                    } 
                    catch 
                    {
                        // Fallback
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch {}
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() => TxtStatus.Text = status);
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                if (destFile.EndsWith(".pdb") || destFile.EndsWith(".xml")) continue;
                File.Copy(file, destFile, true);
            }
            foreach (var sub in Directory.GetDirectories(source))
            {
                string destSub = Path.Combine(dest, Path.GetFileName(sub));
                CopyDirectory(sub, destSub);
            }
        }

        private void CreateShortcut(string name, string target, string destDir, string arguments = "")
        {
            try 
            {
                string link = Path.Combine(destDir, name + ".lnk");
                string workingDir = Path.GetDirectoryName(target);
                string script = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{link}');$s.TargetPath='{target}';$s.Arguments='{arguments}';$s.WorkingDirectory='{workingDir}';$s.IconLocation='{target},0';$s.Save()";
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch {}
        }

        private void CleanupExistingStartupEntries()
        {
            try
            {
                // Remove registry-based startup entries
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        // Remove known registry key names used by this app
                        string[] keysToRemove = { "ActivityTrackerBackground", "Karmetric", "Karmetric.Background" };
                        foreach (var keyName in keysToRemove)
                        {
                            if (key.GetValue(keyName) != null)
                            {
                                key.DeleteValue(keyName, false);
                            }
                        }
                    }
                }

                // Remove old shortcuts from Startup folder
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                foreach (var file in Directory.GetFiles(startupFolder, "Karmetric*.lnk"))
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (var file in Directory.GetFiles(startupFolder, "Karmetric*.lnk"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }



        private void CleanupLegacyUI(string installDir)
        {
            try
            {
                // 1. Delete Legacy UI Folder
                string uiDir = Path.Combine(installDir, "Karmetric.UI");
                if (Directory.Exists(uiDir))
                {
                    UpdateStatus("Removing legacy UI files...");
                    Directory.Delete(uiDir, true);
                }

                // 2. Delete Legacy Desktop Shortcut (.lnk)
                // The new one is .url, so we want to ensure the old .lnk is gone to avoid duplicates
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string legacyLnk = Path.Combine(desktop, "Karmetric.lnk");
                if (File.Exists(legacyLnk))
                {
                    File.Delete(legacyLnk);
                }

                // 3. Delete Legacy Start Menu Shortcut (.lnk)
                string programsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string appStartCtx = Path.Combine(programsDir, "Karmetric");
                if (Directory.Exists(appStartCtx))
                {
                    string legacyStartLnk = Path.Combine(appStartCtx, "Karmetric.lnk");
                    if (File.Exists(legacyStartLnk))
                    {
                        File.Delete(legacyStartLnk);
                    }
                }
            }
            catch (Exception ex)
            {
                // Non-critical
                Console.WriteLine($"Cleanup Warning: {ex.Message}");
            }
        }

        private void RegisterUninstaller(string installDir, string version)
        {
            try
            {
                using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (parent == null) return;

                    using (RegistryKey key = parent.CreateSubKey("Karmetric"))
                    {
                        if (key != null)
                        {
                            string uninstallerPath = Path.Combine(installDir, "Karmetric.Uninstaller.exe");
                            string iconPath = Path.Combine(installDir, "Karmetric.Background.exe"); // Use Background icon

                            key.SetValue("DisplayName", "Karmetric");
                            key.SetValue("ApplicationVersion", version);
                            key.SetValue("Publisher", "Kaustubh Paturi");
                            key.SetValue("DisplayIcon", iconPath);
                            key.SetValue("DisplayVersion", version);
                            key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                            key.SetValue("UninstallString", $"\"{uninstallerPath}\"");
                            key.SetValue("InstallLocation", installDir);
                            key.SetValue("NoModify", 1);
                            key.SetValue("NoRepair", 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register uninstaller: {ex.Message}");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


    }
}