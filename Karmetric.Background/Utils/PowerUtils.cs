using System;
using System.Diagnostics;

namespace Karmetric.Background.Utils
{
    public static class PowerUtils
    {
        public static bool IsSystemKeepAwake()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/requests",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    // Parse Output
                    // Check ALL relevant sections: DISPLAY, SYSTEM, EXECUTION, AWAYMODE
                    return HasRequests(output, "DISPLAY:") || 
                           HasRequests(output, "SYSTEM:") || 
                           HasRequests(output, "EXECUTION:") || 
                           HasRequests(output, "AWAYMODE:");
                }
            }
            catch (Exception)
            {
                // Fallback to false if we can't check
                return false;
            }
        }

        private static bool HasRequests(string output, string sectionHeader)
        {
            int index = output.IndexOf(sectionHeader);
            if (index == -1) return false;

            // Find next section or end
            int nextSectionIndex = -1;
            
            // Typical sections: DISPLAY, SYSTEM, AWAYMODE, EXECUTION, PERFBOOST, ACTIVELOCKSCREEN
            string[] sections = { "DISPLAY:", "SYSTEM:", "AWAYMODE:", "EXECUTION:", "PERFBOOST:", "ACTIVELOCKSCREEN:" };
            
            foreach (var s in sections)
            {
                if (s == sectionHeader) continue;
                int i = output.IndexOf(s, index + 1);
                if (i != -1)
                {
                    if (nextSectionIndex == -1 || i < nextSectionIndex) nextSectionIndex = i;
                }
            }

            string content;
            if (nextSectionIndex != -1)
            {
                content = output.Substring(index + sectionHeader.Length, nextSectionIndex - (index + sectionHeader.Length));
            }
            else
            {
                content = output.Substring(index + sectionHeader.Length);
            }

            // Check if there is anything other than "None." and whitespace
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.Equals("None.", StringComparison.OrdinalIgnoreCase)) continue;
                
                // Found something!
                return true; 
            }

            return false;
        }
    }
}
