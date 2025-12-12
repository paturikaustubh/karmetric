using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Reflection;
using Microsoft.Win32;
using System.Linq;

namespace ActivityMonitor.Installer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            GridWelcome.Visibility = Visibility.Collapsed;
            GridInstalling.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(() => PerformInstall());
                
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

        private void PerformInstall()
        {
            UpdateStatus("Initializing...");

            // Runtime Install Logic (User Request)
            bool installRuntime = false;
            bool createDesktop = true;
            // Strict Monitor is now the ONLY mode as per update v0.1.X - Simple Idle Detection
            // bool strictMonitor = false; // logic removed
            // Dispatcher.Invoke(() => strictMonitor = ChkStrictMonitor.IsChecked == true);

            if (installRuntime)
            {
                UpdateStatus("Downloading .NET 8 Runtime...");
                string runtimeUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe";
                string installerPath = Path.Combine(Path.GetTempPath(), "dotnet_installer.exe");

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(runtimeUrl, installerPath);
                }

                UpdateStatus("Installing Runtime...");
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/install /quiet /norestart", // Silent install
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
                using (var stream = assembly.GetManifestResourceStream("ActivityMonitor.Installer.payload.zip"))
                {
                    if (stream == null) throw new Exception("Installer Payload not found! Build issue.");
                    
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
                string uiSource = Path.Combine(repoRoot, "UI");
                
                // Debug Check
                if (!Directory.Exists(uiSource))
                {
                    var dirs = Directory.GetDirectories(repoRoot);
                    throw new DirectoryNotFoundException($"Extraction failed. 'UI' folder missing in {repoRoot}. Found: {string.Join(", ", dirs)}");
                }
                string appSource = Path.Combine(repoRoot, "application");
                string logoSource = Path.Combine(repoRoot, "logo.svg");

                // Read Version from monitor.json in extracted payload
                string version = "1.0.0"; // Fallback
                try
                {
                    string jsonPath = Path.Combine(repoRoot, "monitor.json");
                    if (File.Exists(jsonPath))
                    {
                        var json = File.ReadAllText(jsonPath);
                        // Simple regex parse to avoid JSON dependency if not needed, or dynamic
                        // But we can just use dynamic or string search for simplicity in .NET 4.8 without Newtonsoft
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

                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ActivityMonitor");

                UpdateStatus("Stopping existing processes...");
                KillProcess("ActivityMonitor.Background");
                KillProcess("ActivityMonitor.UI");
                System.Threading.Thread.Sleep(2000);

                UpdateStatus("Copying Core Files...");
                CopyDirectory(bgSource, targetDir);
                CopyDirectory(uiSource, targetDir);

                UpdateStatus("Copying Frontend Assets...");
                string targetAppDir = Path.Combine(targetDir, "application");
                if (Directory.Exists(targetAppDir)) Directory.Delete(targetAppDir, true);
                CopyDirectory(appSource, targetAppDir);
                
                // Copy Logo (SVG) for UI use
                if (File.Exists(logoSource))
                {
                    File.Copy(logoSource, Path.Combine(targetAppDir, "logo.svg"), true);
                }

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
                string bgExe = Path.Combine(targetDir, "ActivityMonitor.Background.exe");
                
                // Clean up all existing startup registrations to prevent duplicates
                CleanupExistingStartupEntries();
                
                UpdateStatus("Registering Auto-Start...");
                // Add to Startup Folder only (not registry to avoid duplicates)
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                CreateShortcut("Activity Monitor Background", bgExe, startupFolder);
                
                UpdateStatus("Starting background service...");
                var psi = new ProcessStartInfo
                {
                    FileName = bgExe,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
                
                UpdateStatus("Creating Shortcuts...");
                
                // 1. Desktop Shortcut (User Option)
                if (createDesktop)
                {
                    CreateShortcut("Activity Monitor", Path.Combine(targetDir, "ActivityMonitor.UI.exe"), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
                }

                // 2. Start Menu Shortcuts (Required for Pin/Search/Uninstall)
                string programsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string appStartMenuDir = Path.Combine(programsDir, "Activity Monitor");
                if (!Directory.Exists(appStartMenuDir)) Directory.CreateDirectory(appStartMenuDir);

                CreateShortcut("Activity Monitor", Path.Combine(targetDir, "ActivityMonitor.UI.exe"), appStartMenuDir);
                
                // Copy Uninstaller files (Robust Search)
                // Search recursively in case user put it in a subfolder
                var uninstallerExe = Directory.GetFiles(repoRoot, "ActivityMonitor.Uninstaller.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (uninstallerExe != null)
                {
                    string sourceDir = Path.GetDirectoryName(uninstallerExe);
                    // Copy detection: .exe, .dll, .json
                    var filesToCopy = Directory.GetFiles(sourceDir, "ActivityMonitor.Uninstaller.*");
                    
                    foreach(var f in filesToCopy)
                    {
                        string fName = Path.GetFileName(f);
                        File.Copy(f, Path.Combine(targetDir, fName), true);
                    }

                    UpdateStatus("Registering Uninstaller...");
                    RegisterUninstaller(targetDir, version);

                    // Add Uninstall Shortcut to Start Menu
                    string uninstallerPath = Path.Combine(targetDir, "ActivityMonitor.Uninstaller.exe");
                    CreateShortcut("Uninstall Activity Monitor", uninstallerPath, appStartMenuDir);
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

        private void CreateShortcut(string name, string target, string destDir)
        {
            try 
            {
                string link = Path.Combine(destDir, name + ".lnk");
                string script = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{link}');$s.TargetPath='{target}';$s.IconLocation='{target},0';$s.Save()";
                
                // Assuming .ico is next to .exe? No, checking logic.
                // We copied ActivityMonitor.ico to targetDir.
                // Shortcuts usually pick up exe icon if embedded.
                // But I can explicit set it if I want.
                // Let's stick to simplest script but ensuring DestDir is used.
                
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
                        string[] keysToRemove = { "ActivityTrackerBackground", "ActivityMonitor", "ActivityMonitor.Background" };
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
                foreach (var file in Directory.GetFiles(startupFolder, "Activity Monitor*.lnk"))
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (var file in Directory.GetFiles(startupFolder, "ActivityMonitor*.lnk"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }



        private void RegisterUninstaller(string installDir, string version)
        {
            try
            {
                using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (parent == null) return;

                    using (RegistryKey key = parent.CreateSubKey("ActivityMonitor"))
                    {
                        if (key != null)
                        {
                            string uninstallerPath = Path.Combine(installDir, "ActivityMonitor.Uninstaller.exe");
                            string iconPath = Path.Combine(installDir, "ActivityMonitor.UI.exe"); // Use UI icon

                            key.SetValue("DisplayName", "Activity Monitor");
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