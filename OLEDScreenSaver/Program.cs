using System;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Infrastructure services
            ILogger logger = new FileLogger();
            IScreenService screenService = new WindowsScreenService(logger);
            ICursorService cursorService = new WindowsCursorService();
            IWindowManager windowManager = new WindowsWindowManager();

            // Set DPI awareness early
            screenService.SetProcessDpiAware();

            // Component services
            IConfigurationRepository configRepo = new RegistryConfigurationRepository(logger);
            IUserActivityMonitor activityMonitor = new Win32UserActivityMonitor(configRepo, logger, screenService);
            IScreenSaverManager screenSaverManager = new ScreenSaverManager(configRepo, activityMonitor, logger, screenService);
            
            // Managers
            OledFormManager oledFormManager = new OledFormManager(
                configRepo, 
                screenSaverManager, 
                logger, 
                screenService, 
                cursorService, 
                windowManager);

            // Factory for ConfigForm to ensure it's created with fresh dependencies
            Func<ConfigForm> configFormFactory = () => new ConfigForm(
                configRepo, 
                screenSaverManager, 
                oledFormManager, 
                logger, 
                screenService);

            // Application Context
            TrayApplicationContext context = new TrayApplicationContext(
                configRepo, 
                screenSaverManager, 
                oledFormManager, 
                configFormFactory);

            Application.Run(context);
        }
    }
}
