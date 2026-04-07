using System;

namespace OLEDScreenSaver
{
    public interface IWindowManager
    {
        void ShowNoActivate(IntPtr handle);
        void HideFromAltTab(IntPtr handle);
    }

}
