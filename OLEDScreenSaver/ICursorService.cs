using System;

namespace OLEDScreenSaver
{
    public interface ICursorService : IDisposable
    {
        void Show();
        void Hide();
        IntPtr GetBlankCursor();
    }
}
