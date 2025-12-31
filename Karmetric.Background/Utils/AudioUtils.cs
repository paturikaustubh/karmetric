using System;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
namespace Karmetric.Background.Utils
{
    public static class AudioUtils
    {
        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int dwStateMask, out object ppDevices);
            [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr pActivationParams, out object ppInterface);
        }

        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig] int GetPeakValue(out float pfPeak);
        }

        [Guid("77AA99A0-1BD6-484F-8BC2-3C30E2734581"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            [PreserveSig] int GetAudioSessionControl(Guid AudioSessionGuid, int StreamFlags, out object SessionControl);
            [PreserveSig] int GetSimpleAudioVolume(Guid AudioSessionGuid, int StreamFlags, out object AudioVolume);
            [PreserveSig] int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
        }

        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            [PreserveSig] int GetCount(out int SessionCount);
            [PreserveSig] int GetSession(int SessionCount, out IAudioSessionControl Session);
        }

        [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl
        {
            [PreserveSig] int GetState(out AudioSessionState pRetVal);
            [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        }

        private enum AudioSessionState
        {
            Inactive = 0,
            Active = 1,
            Expired = 2
        }

        public static bool IsAudioSessionActive(ILogger logger = null)
        {
            try
            {
                var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
                if (enumerator == null) return false;

                // Check Render (Speakers)
                if (CheckSessionActive(enumerator, 0, 0, logger, "Render-Console")) return true;
                if (CheckSessionActive(enumerator, 0, 2, logger, "Render-Comm")) return true;

                // Check Capture (Mic)
                if (CheckSessionActive(enumerator, 1, 0, logger, "Capture-Console")) return true;
                if (CheckSessionActive(enumerator, 1, 2, logger, "Capture-Comm")) return true;
                
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking audio sessions");
                return false;
            }
        }

        private static bool CheckSessionActive(IMMDeviceEnumerator enumerator, int dataFlow, int role, ILogger logger, string description)
        {
            try 
            {
                enumerator.GetDefaultAudioEndpoint(dataFlow, role, out var device);
                if (device == null) return false;

                var iidCtx = typeof(IAudioSessionManager2).GUID;
                device.Activate(iidCtx, 0, IntPtr.Zero, out var obj);
                
                var manager = obj as IAudioSessionManager2;
                if (manager == null) return false;

                manager.GetSessionEnumerator(out var sessionEnum);
                if (sessionEnum == null) return false;

                sessionEnum.GetCount(out int count);
                for (int i = 0; i < count; i++)
                {
                    sessionEnum.GetSession(i, out var session);
                    if (session != null)
                    {
                        session.GetState(out var state);
                        
                        // Try to get process ID or display name for debugging
                        string displayName = "Unknown";
                        try { session.GetDisplayName(out displayName); } catch {}
                        
                        // Log everything at INFO level to ensure we see it
                        logger?.LogInformation($"Audio Session Check [{description}] - State: {state}, Name: {displayName}");

                        if (state == AudioSessionState.Active)
                        {
                            logger?.LogInformation($"!!! ACTIVE SESSION FOUND !!!: {displayName} on {description}");
                            Marshal.ReleaseComObject(session);
                            return true;
                        }
                        Marshal.ReleaseComObject(session);
                    }
                }
                Marshal.ReleaseComObject(sessionEnum);
                Marshal.ReleaseComObject(manager);
                Marshal.ReleaseComObject(device);
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to check session for {description}: {ex.Message}");
                return false;
            }
        }

        // Kept for legacy/fallback if needed, though IsAudioSessionActive is preferred
        public static float GetAudioPeak()
        {
            float maxPeak = 0f;
            try
            {
                var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
                if (enumerator == null) return 0f;

                maxPeak = Math.Max(maxPeak, GetPeak(enumerator, 0, 0));
                maxPeak = Math.Max(maxPeak, GetPeak(enumerator, 0, 2));
                maxPeak = Math.Max(maxPeak, GetPeak(enumerator, 1, 0));
                maxPeak = Math.Max(maxPeak, GetPeak(enumerator, 1, 2));
                
                return maxPeak;
            }
            catch
            {
                return maxPeak;
            }
        }

        private static float GetPeak(IMMDeviceEnumerator enumerator, int dataFlow, int role)
        {
            try 
            {
                enumerator.GetDefaultAudioEndpoint(dataFlow, role, out var device);
                if (device == null) return 0f;

                var iidCtx = typeof(IAudioMeterInformation).GUID;
                device.Activate(iidCtx, 0, IntPtr.Zero, out var obj);
                
                var meter = obj as IAudioMeterInformation;
                if (meter == null) return 0f;

                meter.GetPeakValue(out var peak);
                
                Marshal.ReleaseComObject(meter);
                Marshal.ReleaseComObject(device);
                return peak;
            }
            catch
            {
                return 0f;
            }
        }
    }

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
