using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace Karmetric.Background.Utils
{
    public static class AutoStartUtils
    {
        private const string AppName = "ActivityTrackerBackground";

        public static void EnsureAutoStart()
        {
            try 
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                // If running as dotnet.exe (during dev), don't register. 
                // Only register if it's the published .exe or specific project exe
                if (exePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)) return;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        var existingValue = key.GetValue(AppName);
                        if (existingValue == null || existingValue.ToString() != exePath)
                        {
                            key.SetValue(AppName, exePath);
                            Console.WriteLine($"Auto-start registered: {exePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register auto-start: {ex.Message}");
            }
        }
    }
}
