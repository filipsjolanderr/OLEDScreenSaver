using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Windows;

namespace OLEDScreenSaver
{
    class ScreenHelper
    {
        public static Screen FindScreen(String name)
        {
            var screens = Screen.AllScreens;
            var friendlyNames = ScreenInterrogatory.GetAllMonitorsFriendlyNames().ToList();
            
            for (var i = 0; i < screens.Length; i++)
            {
                if (i < friendlyNames.Count)
                {
                    if (friendlyNames[i] == name)
                    {
                        return screens[i];
                    }
                }
            }
            LogHelper.Log("Screen name was not found: " + name);
            return null;
        }

        public static int getNumberOfConnectedMonitors()
        {
            var numberOfMonitors = 1;

            //Detect number of monitors. However, this does NOT work when no monitors are connected. It instead gives a 1.
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity where service =\"monitor\"");
            numberOfMonitors = searcher.Get().Count;

            //Get's the monitor's instance name. "Default_Monitor" is the "monitor" Windows defaults to when nothing is connected
            var activeScreen = "";
            using (var wmiSearcher = new ManagementObjectSearcher("\\root\\wmi", "select * from WmiMonitorBasicDisplayParams"))
            {
                var results = wmiSearcher.Get();
                foreach (ManagementObject wmiObj in results)
                {
                    // tell us if the display is active
                    var active = (Boolean)wmiObj["Active"];
                    //Get the instance name of the active monitor
                    if (active)
                    {
                        activeScreen = (string)wmiObj["InstanceName"];
                    }
                }
            }

            //If Windows says only one monitor is connected and that monitor is Default_Monitor, then that means that there are no monitors detected by Windows
            if (numberOfMonitors == 1 && activeScreen.Contains("Default_Monitor"))
            {
                numberOfMonitors = 0;
            }

            return numberOfMonitors;
        }

        private enum ProcessDPIAwareness
        {
            ProcessDPIUnaware = 0,
            ProcessSystemDPIAware = 1,
            ProcessPerMonitorDPIAware = 2
        }

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);

        public static void SetDpiAwareness()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDpiAwareness(ProcessDPIAwareness.ProcessPerMonitorDPIAware);
                }
            }
            catch (EntryPointNotFoundException)//this exception occures if OS does not implement this API, just ignore it.
            {
            }
        }
        public enum PowerMgmt
        {
            StandBy,
            Off,
            On
        };

        public class ScreenPowerMgmtEventArgs
        {
            private PowerMgmt _PowerStatus;
            public ScreenPowerMgmtEventArgs(PowerMgmt powerStat)
            {
                this._PowerStatus = powerStat;
            }
            public PowerMgmt PowerStatus
            {
                get { return this._PowerStatus; }
            }
        }
        public class ScreenPowerMgmt
        {
            public ScreenPowerMgmt() { }
            public delegate void ScreenPowerMgmtEventHandler(object sender, ScreenPowerMgmtEventArgs e);
            public event ScreenPowerMgmtEventHandler ScreenPower;
            private void OnScreenPowerMgmtEvent(ScreenPowerMgmtEventArgs args)
            {
                if (this.ScreenPower != null) this.ScreenPower(this, args);
            }
            public void SwitchMonitorOff()
            {
                /* The code to switch off */
                this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.Off));
            }
            public void SwitchMonitorOn()
            {
                /* The code to switch on */
                this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.On));
            }
            public void SwitchMonitorStandby()
            {
                /* The code to switch standby */
                this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.StandBy));
            }

        }


    }

}
