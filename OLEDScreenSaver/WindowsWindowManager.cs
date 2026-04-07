using System;

namespace OLEDScreenSaver
{
    public class WindowsWindowManager : IWindowManager
    {
        public void ShowNoActivate(IntPtr handle)
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOWNOACTIVATE);
        }

        public void HideFromAltTab(IntPtr handle)
        {
            var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);
        }
    }
}
