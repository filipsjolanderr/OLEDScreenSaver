using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public interface IScreenService
    {
        Screen FindScreenByName(string name);
        int GetConnectedMonitorCount();
        void SetProcessDpiAware();
        IEnumerable<string> GetAllMonitorsFriendlyNames();
    }
}
