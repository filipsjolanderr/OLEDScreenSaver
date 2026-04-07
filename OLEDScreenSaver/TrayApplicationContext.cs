using System;
using System.Drawing;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly IConfigurationRepository _configRepository;
        private readonly IScreenSaverManager _screenSaverManager;
        private readonly OledFormManager _oledFormManager;
        private readonly Func<ConfigForm> _configFormFactory;

        private NotifyIcon _notifyIcon;
        private ContextMenu _contextMenu;
        
        public TrayApplicationContext(
            IConfigurationRepository configRepository, 
            IScreenSaverManager screenSaverManager, 
            OledFormManager oledFormManager,
            Func<ConfigForm> configFormFactory)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _screenSaverManager = screenSaverManager ?? throw new ArgumentNullException(nameof(screenSaverManager));
            _oledFormManager = oledFormManager ?? throw new ArgumentNullException(nameof(oledFormManager));
            _configFormFactory = configFormFactory ?? throw new ArgumentNullException(nameof(configFormFactory));

            InitializeTrayIcon();

            var messageWindow = new MessageWindow(this);
            NativeMethods.RegisterHotKey(messageWindow.Handle, 1, NativeMethods.MOD_WIN | NativeMethods.MOD_SHIFT, (int)Keys.L);

            _screenSaverManager.Launch();
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenu();

            var pauseMenu = new MenuItem("Pause");
            pauseMenu.MenuItems.Add(new MenuItem("Resume", (s, e) => { _screenSaverManager.Resume(); UpdatePauseMenu(true); }));
            pauseMenu.MenuItems.Add(new MenuItem("For 30 minutes", (s, e) => { _screenSaverManager.Pause(30); UpdatePauseMenu(true); }));
            pauseMenu.MenuItems.Add(new MenuItem("For 1 hour", (s, e) => { _screenSaverManager.Pause(60); UpdatePauseMenu(true); }));
            pauseMenu.MenuItems.Add(new MenuItem("For 2 hours", (s, e) => { _screenSaverManager.Pause(120); UpdatePauseMenu(true); }));
            pauseMenu.MenuItems.Add(new MenuItem("Indefinitely", (s, e) => { _screenSaverManager.Pause(); UpdatePauseMenu(true); }));
            
            _contextMenu.MenuItems.Add(pauseMenu);
            _contextMenu.MenuItems.Add(new MenuItem("Config", (s, e) => ShowConfig()));
            _contextMenu.MenuItems.Add(new MenuItem("Exit", (s, e) => ExitApplication()));

            _notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.Alecive_Flatwoken_Apps_Computer_Screensaver,
                ContextMenu = _contextMenu,
                Visible = true,
                Text = "OLED Screen Saver"
            };

            UpdatePauseMenu(false);
        }

        private void UpdatePauseMenu(bool isPaused)
        {
            var pauseMenu = _contextMenu.MenuItems[0];
            pauseMenu.MenuItems[0].Enabled = isPaused;
            for (int i = 1; i < pauseMenu.MenuItems.Count; i++) pauseMenu.MenuItems[i].Enabled = !isPaused;
        }

        private void ShowConfig()
        {
            using (var configForm = _configFormFactory())
            {
                if (configForm.ShowDialog() == DialogResult.Yes)
                {
                    _screenSaverManager.ReloadConfiguration();
                    _oledFormManager.RefreshOLEDForms();
                }
            }
        }

        private void ExitApplication()
        {
            _notifyIcon.Visible = false;
            _screenSaverManager.Dispose();
            _oledFormManager.Dispose();
            Application.Exit();
        }

        private class MessageWindow : Form
        {
            private readonly TrayApplicationContext _context;
            public MessageWindow(TrayApplicationContext context) { _context = context; }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == 1) _context._screenSaverManager.ToggleScreensaver();
                base.WndProc(ref m);
            }
            protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);
        }
    }
}
