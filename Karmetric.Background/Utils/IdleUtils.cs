using System;
using System.Runtime.InteropServices;

namespace Karmetric.Background.Utils
{
    public static class IdleUtils 
    {
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public static uint GetIdleTimeSeconds()
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);

            if (GetLastInputInfo(ref lii))
            {
                var tickCount = (uint)Environment.TickCount;
                if (tickCount >= lii.dwTime)
                {
                    return (tickCount - lii.dwTime) / 1000;
                }
            }
            return 0;
        }
    }
}
