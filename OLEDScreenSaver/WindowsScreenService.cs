using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Management;

namespace OLEDScreenSaver
{
    public class WindowsScreenService : IScreenService
    {
        private readonly ILogger _logger;

        public WindowsScreenService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Screen FindScreenByName(string name)
        {
            var screens = Screen.AllScreens;
            var friendlyNames = GetAllMonitorsFriendlyNames().ToList();
            
            for (var i = 0; i < screens.Length; i++)
            {
                if (i < friendlyNames.Count && friendlyNames[i] == name)
                {
                    return screens[i];
                }
            }
            
            _logger.Log($"Screen name was not found: {name}");
            return null;
        }

        public IEnumerable<string> GetAllMonitorsFriendlyNames()
        {
            uint pathCount, modeCount;
            var error = NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
            if (error != 0) yield break;

            var displayPaths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            var displayModes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
            error = NativeMethods.QueryDisplayConfig(NativeMethods.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, displayPaths, ref modeCount, displayModes, IntPtr.Zero);
            if (error != 0) yield break;

            for (var i = 0; i < modeCount; i++)
            {
                if (displayModes[i].infoType == NativeMethods.DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
                {
                    var deviceName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME
                    {
                        header =
                        {
                            size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME)),
                            adapterId = displayModes[i].adapterId,
                            id = displayModes[i].id,
                            type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME
                        }
                    };
                    if (NativeMethods.DisplayConfigGetDeviceInfo(ref deviceName) == 0)
                    {
                        yield return deviceName.monitorFriendlyDeviceName;
                    }
                }
            }
        }

        public int GetConnectedMonitorCount()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity where service =\"monitor\"");
                var numberOfMonitors = searcher.Get().Count;

                var activeScreen = "";
                using (var wmiSearcher = new ManagementObjectSearcher("\\root\\wmi", "select * from WmiMonitorBasicDisplayParams"))
                {
                    foreach (ManagementObject wmiObj in wmiSearcher.Get())
                    {
                        if ((bool)wmiObj["Active"])
                        {
                            activeScreen = (string)wmiObj["InstanceName"];
                        }
                    }
                }

                if (numberOfMonitors == 1 && activeScreen.Contains("Default_Monitor"))
                {
                    return 0;
                }

                return numberOfMonitors;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get connected monitor count", ex);
                return Screen.AllScreens.Length;
            }
        }

        public void SetProcessDpiAware()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    // 2 = ProcessPerMonitorDPIAware
                    NativeMethods.SetProcessDpiAwareness(2);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Could not set DPI awareness: {ex.Message}");
            }
        }
    }
}
