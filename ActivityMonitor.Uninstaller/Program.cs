using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Threading;

namespace ActivityMonitor.Uninstaller
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 1. Confirm Uninstall
            var result = MessageBox.Show("Are you sure you want to completely remove Activity Monitor and all its data?", 
                                         "Uninstall Activity Monitor", 
                                         MessageBoxButtons.YesNo, 
                                         MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                // 2. Stop Processes
                KillProcess("ActivityMonitor.Background");
                KillProcess("ActivityMonitor.UI");
                KillProcess("ActivityMonitor.Installer"); // Just in case
                Thread.Sleep(2000);

                // 3. Clean Registry
                CleanRegistry();

                // 4. Remove Shortcuts
                RemoveShortcuts();

                // 5. Remove Files (Current Directory logic is tricky if we are IN it)
                // Using a temp script for self-deletion and folder removal
                string installDir = AppDomain.CurrentDomain.BaseDirectory;
                string tempDir = Path.GetTempPath();
                string batchScript = Path.Combine(tempDir, "am_uninstall_cleanup.bat");

                // Create a batch script that waits, tries to delete the dir, and then deletes itself
                // We use Ping for a small delay to let this process exit
                string scriptContent = $@"
@echo off
timeout /t 3 /nobreak > NUL
rd /s /q ""{installDir}""
del ""%~f0""
";
                File.WriteAllText(batchScript, scriptContent);

                // 6. Launch Cleanup Script
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchScript,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uninstall Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void KillProcess(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    p.Kill();
                    p.WaitForExit(1000);
                }
            }
            catch { }
        }

        static void CleanRegistry()
        {
            try
            {
                // Auto-Start
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("ActivityTrackerBackground", false);
                        key.DeleteValue("ActivityMonitor", false);
                        key.DeleteValue("ActivityMonitor.Background", false);
                    }
                }

                // Uninstall Entry
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("ActivityMonitor", false);
                    }
                }
            }
            catch { }
        }

        static void RemoveShortcuts()
        {
            try
            {
                string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                DeleteFile(Path.Combine(startup, "Activity Monitor Background.lnk"));
                DeleteFile(Path.Combine(startup, "Activity Monitor.lnk"));
                DeleteFile(Path.Combine(desktop, "Activity Monitor.lnk"));
            }
            catch { }
        }

        static void DeleteFile(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
