using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Threading;
using System.Drawing;

namespace Karmetric.Uninstaller
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallForm());
        }

        public static void RunUninstall(bool keepData)
        {
            try
            {
                // 1. Stop Processes
                KillProcess("Karmetric.Background");
                KillProcess("Karmetric.UI");
                KillProcess("Karmetric.Installer");
                Thread.Sleep(2000);

                // 2. Clean Registry
                CleanRegistry();

                // 3. Remove Shortcuts
                RemoveShortcuts();

                // 4. Cleanup Files via Script
                string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string tempDir = Path.GetTempPath().TrimEnd('\\');
                string psScript = Path.Combine(tempDir, "am_uninstall_cleanup.ps1");

                string scriptContent;
                
                if (keepData)
                {
                    // Logic: Delete everything EXCEPT ActivityLog.db
                    scriptContent = $@"
$ErrorActionPreference = 'SilentlyContinue'
Start-Sleep -Seconds 3
$installDir = '{installDir}'

if (Test-Path -LiteralPath $installDir) {{
    Get-ChildItem -LiteralPath $installDir | Where-Object {{ $_.Name -ne 'ActivityLog.db' }} | Remove-Item -Recurse -Force
}}

# Delete script itself
Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force
";
                }
                else
                {
                    // Logic: Delete Everything
                    scriptContent = $@"
$ErrorActionPreference = 'SilentlyContinue'
Start-Sleep -Seconds 3
$installDir = '{installDir}'

if (Test-Path -LiteralPath $installDir) {{
    Remove-Item -LiteralPath $installDir -Recurse -Force
}}

# Delete script itself
Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force
";
                }

                File.WriteAllText(psScript, scriptContent);

                // 5. Launch Cleanup Script
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{psScript}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
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
                        key.DeleteValue("Karmetric", false);
                        key.DeleteValue("Karmetric.Background", false);
                    }
                }

                // Uninstall Entry
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("Karmetric", false);
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
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

                DeleteFile(Path.Combine(startup, "Karmetric Background.lnk"));
                DeleteFile(Path.Combine(startup, "Karmetric.lnk"));
                DeleteFile(Path.Combine(desktop, "Karmetric.lnk"));
                
                // Remove Start Menu Folder
                string appStartCtx = Path.Combine(programs, "Karmetric");
                if (Directory.Exists(appStartCtx)) Directory.Delete(appStartCtx, true);
            }
            catch { }
        }

        static void DeleteFile(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // Simple Form for Uninstall Confirmation
    public class UninstallForm : Form
    {
        public bool KeepData { get; private set; }
        private CheckBox chkKeepData;
        private Button btnUninstall;
        private Button btnCancel;
        private Label lblMessage;

        public UninstallForm()
        {
            this.Text = "Uninstall Karmetric";
            this.Size = new Size(400, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblMessage = new Label
            {
                Text = "Are you sure you want to remove Karmetric?",
                AutoSize = false,
                Size = new Size(360, 40),
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 10)
            };

            chkKeepData = new CheckBox
            {
                Text = "Keep session history",
                AutoSize = true,
                Location = new Point(20, 70),
                Checked = false,
                Font = new Font("Segoe UI", 9)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.None,
                Location = new Point(180, 120),
                Size = new Size(90, 30)
            };
            btnCancel.Click += (s, e) => Application.Exit();

            btnUninstall = new Button
            {
                Text = "Uninstall",
                DialogResult = DialogResult.None, // Handled manually
                Location = new Point(280, 120), // Moved to right
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUninstall.FlatAppearance.BorderSize = 0;
            btnUninstall.Click += BtnUninstall_Click;

            this.Controls.AddRange(new Control[] { lblMessage, chkKeepData, btnCancel, btnUninstall });
            this.AcceptButton = btnUninstall;
            this.CancelButton = btnCancel;
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            // Update UI
            lblMessage.Text = "Uninstalling... Please wait.";
            btnUninstall.Enabled = false;
            btnCancel.Enabled = false;
            chkKeepData.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents(); // Force redraw

            // Run Logic
            Program.RunUninstall(chkKeepData.Checked);

            this.Cursor = Cursors.Default;
            MessageBox.Show("Karmetric has been successfully uninstalled.", "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();
        }
    }
}
