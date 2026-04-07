using System;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    // Custom form class that hides from Alt+Tab and handles screen positioning properly
    public class OLEDForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                int WS_EX_TOOLWINDOW = 0x80;
                int WS_EX_TRANSPARENT = 0x20;
                // Hide from Alt+Tab switcher and make completely click-through
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT; 
                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            // Ensure visibility changes happen on the UI thread to avoid cross-thread exceptions
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(v => base.SetVisibleCore(v)), value);
                return;
            }

            base.SetVisibleCore(value);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Ensure form stays on top and covers the screen
            // Don't call BringToFront() - it can steal focus
            this.TopMost = true;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }
    }
}
