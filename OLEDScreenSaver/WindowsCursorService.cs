using System;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public class WindowsCursorService : ICursorService
    {
        private static IntPtr _blankCursor = IntPtr.Zero;

        public IntPtr GetBlankCursor()
        {
            if (_blankCursor == IntPtr.Zero)
            {
                var andMask = new byte[] { 0xFF };
                var xorMask = new byte[] { 0x00 };
                _blankCursor = NativeMethods.CreateCursor(IntPtr.Zero, 0, 0, 1, 1, andMask, xorMask);
            }
            return _blankCursor;
        }

        public void Show()
        {
            NativeMethods.SetCursor(NativeMethods.LoadCursor(IntPtr.Zero, 32512)); // IDC_ARROW
            while (NativeMethods.ShowCursor(true) < 0) { }
        }

        public void Hide()
        {
            NativeMethods.SetCursor(IntPtr.Zero);
            NativeMethods.SetCursor(GetBlankCursor());
            while (NativeMethods.ShowCursor(false) >= 0) { }
        }

        public void Dispose()
        {
            if (_blankCursor != IntPtr.Zero)
            {
                NativeMethods.DestroyCursor(_blankCursor);
                _blankCursor = IntPtr.Zero;
            }
        }
    }
}
