using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using System.Windows.Interop;

// Icon by alecive from https://iconarchive.com/show/flatwoken-icons-by-alecive/Apps-Computer-Screensaver-icon.html

namespace OLEDScreenSaver
{
    public partial class MainForm : Form
    {
        private readonly ContextMenu contextMenu1;
        private readonly MenuItem menuItem1;
        private readonly MenuItem menuItem2;
        private readonly MenuItem menuItem3;
        private readonly ScreenSaver screenSaver;
        private readonly Dictionary<string, OLEDForm> oledForms = new Dictionary<string, OLEDForm>();
        private readonly object formsLock = new object();
        private readonly Timer mouseActivityTimer;
        private Point lastMousePosition = Point.Empty;
        // Animation tracking: screen name -> animation timer
        private readonly Dictionary<string, System.Windows.Forms.Timer> opacityAnimations = new Dictionary<string, System.Windows.Forms.Timer>();
        // Track which screens are fully black (second stage) for cursor hiding
        private readonly Dictionary<string, bool> fullyBlackScreens = new Dictionary<string, bool>();
        // Track current cursor visibility state to avoid unnecessary ShowCursor/HideCursor calls
        private bool cursorCurrentlyHidden = false;
        // Minimum distance in pixels for mouse movement to count as activity (prevents jitter from resetting timer)
        private const int MOUSE_MOVEMENT_THRESHOLD = 5;
        // Track last time we notified activity per screen to prevent too-frequent resets
        private readonly Dictionary<string, DateTime> lastActivityNotification = new Dictionary<string, DateTime>();
        // Minimum time between activity notifications (prevents rapid resets)
        private const int ACTIVITY_NOTIFICATION_COOLDOWN_MS = 1000; // 1 second

        // Track the last global input time to detect keyboard activity
        private uint lastInputTick = 0;
        private DateTime ignoreInputUntil = DateTime.MinValue;

        void MonitorOnChanged(object sender, EventArgs e)
        {
            LogHelper.Log(string.Format("Monitor status changed (new status: {0})", PowerManager.IsMonitorOn ? "On" : "Off"));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Hide from Alt+Tab switcher
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            // Never show the main form - it should always be hidden
            base.SetVisibleCore(false);
        }

        public MainForm()
        {
            InitializeComponent();
            RegistryHelper.InitValues();

            // Ensure form is completely hidden and minimized
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Size = new Size(1, 1); // Make it tiny
            this.Location = new Point(-1000, -1000); // Position off-screen
            this.Opacity = 0; // Make it invisible
            this.notifyIcon1.Icon = SystemIcons.Application;
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();

            this.contextMenu1.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { this.menuItem1, this.menuItem2, this.menuItem3 });

            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Pause";
            
            var miResume = new MenuItem("Resume", MenuItem1_Click);
            var miPause30 = new MenuItem("For 30 minutes", (s, e) => { screenSaver.PauseScreensaver(30); UpdatePauseMenu(true); });
            var miPause60 = new MenuItem("For 1 hour", (s, e) => { screenSaver.PauseScreensaver(60); UpdatePauseMenu(true); });
            var miPause120 = new MenuItem("For 2 hours", (s, e) => { screenSaver.PauseScreensaver(120); UpdatePauseMenu(true); });
            var miPauseIndef = new MenuItem("Indefinitely", (s, e) => { screenSaver.PauseScreensaver(); UpdatePauseMenu(true); });

            this.menuItem1.MenuItems.AddRange(new[] { miResume, miPause30, miPause60, miPause120, miPauseIndef });

            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Config";
            this.menuItem2.Click += new System.EventHandler(this.MenuItem2_Click);

            this.menuItem3.Index = 2;
            this.menuItem3.Text = "Exit";
            this.menuItem3.Click += new System.EventHandler(this.MenuItem3_Click);
            this.notifyIcon1.ContextMenu = this.contextMenu1;
            notifyIcon1.Visible = true;
            this.notifyIcon1.Icon = new Icon(Properties.Resources.Alecive_Flatwoken_Apps_Computer_Screensaver, 40, 40);

            Icon = Properties.Resources.Alecive_Flatwoken_Apps_Computer_Screensaver;

            screenSaver = new ScreenSaver();
            screenSaver.RegisterHideFormCallback(HideFormCallback);
            screenSaver.RegisterShowFormCallback(ShowFirstStageCallback);
            screenSaver.RegisterSecondStageCallback(ShowSecondStageCallback);
            screenSaver.Launch();

            // Ensure form is hidden immediately
            Hide();
            this.Visible = false;

            // Initialize OLED forms once
            RefreshOLEDForms();

            // Start mouse activity tracking timer for OLED screens
            mouseActivityTimer = new Timer
            {
                Interval = RegistryHelper.LoadPollRate()
            };
            mouseActivityTimer.Tick += MouseActivityTimer_Tick;
            mouseActivityTimer.Start();

            // Register Win+Shift+L as Global Hotkey
            Win32Helper.RegisterHotKey(this.Handle, 1, Win32Helper.MOD_WIN | Win32Helper.MOD_SHIFT, (int)Keys.L);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32Helper.WM_HOTKEY && m.WParam.ToInt32() == 1)
            {
                ToggleScreensaver();
            }
            base.WndProc(ref m);
        }

        private void ToggleScreensaver()
        {
            lock (formsLock)
            {
                bool allDisplayed = fullyBlackScreens.Count > 0 && oledForms.Keys.All(k => fullyBlackScreens.ContainsKey(k) && fullyBlackScreens[k]);
                if (allDisplayed)
                {
                    foreach (var scr in oledForms.Keys.ToList())
                    {
                        screenSaver.NotifyOledMouseActivity(scr);
                    }
                }
                else
                {
                    foreach (var scr in oledForms.Keys.ToList())
                    {
                        screenSaver.ForceShowScreensaver(scr);
                    }
                }
            }
            ignoreInputUntil = DateTime.Now.AddSeconds(1); // Ignore input for 1 second after hotkey
        }

        private void MouseActivityTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var cursorPosition = Cursor.Position;

                if (DateTime.Now < ignoreInputUntil)
                {
                    lastMousePosition = cursorPosition;
                    var lii = new Win32Helper.LASTINPUTINFO();
                    lii.cbSize = (uint)Marshal.SizeOf(lii);
                    lii.dwTime = 0;
                    if (Win32Helper.GetLastInputInfo(ref lii))
                    {
                        lastInputTick = lii.dwTime;
                    }
                    return; // Ignore all input shortly after hotkey
                }

                // Only notify activity if mouse has moved a significant distance (prevents jitter from resetting timer)
                var mouseMoved = false;
                if (lastMousePosition != Point.Empty)
                {
                    var deltaX = Math.Abs(cursorPosition.X - lastMousePosition.X);
                    var deltaY = Math.Abs(cursorPosition.Y - lastMousePosition.Y);
                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    mouseMoved = distance >= MOUSE_MOVEMENT_THRESHOLD;
                }
                else
                {
                    // First time, initialize position
                    mouseMoved = false;
                }
                lastMousePosition = cursorPosition;

                // Track which screen the mouse is on and manage cursor visibility
                string mouseScreenName = null;
                var mouseOnFullyBlackScreen = false;

                lock (formsLock)
                {
                    foreach (var kvp in oledForms)
                    {
                        var screenName = kvp.Key;
                        var form = kvp.Value;
                        if (form != null && !form.IsDisposed && form.Bounds.Contains(cursorPosition))
                        {
                            mouseScreenName = screenName;

                            // Check if this screen is fully black (second stage)
                            // Also verify the form is visible and opacity is high enough (>= 0.95 to account for animation)
                            var isMarkedFullyBlack = fullyBlackScreens.ContainsKey(screenName) && fullyBlackScreens[screenName];
                            var isActuallyFullyBlack = isMarkedFullyBlack &&
                                                       form != null &&
                                                       !form.IsDisposed &&
                                                       form.Opacity >= 0.95;

                            mouseOnFullyBlackScreen = isActuallyFullyBlack;

                            // Only update activity if mouse has moved AND is on an OLED screen
                            // Also enforce a cooldown to prevent rapid resets from tiny movements
                            if (mouseMoved)
                            {
                                var shouldNotify = true;
                                if (lastActivityNotification.ContainsKey(screenName))
                                {
                                    var timeSinceLastNotification = (DateTime.Now - lastActivityNotification[screenName]).TotalMilliseconds;
                                    if (timeSinceLastNotification < ACTIVITY_NOTIFICATION_COOLDOWN_MS)
                                    {
                                        shouldNotify = false;
                                    }
                                }

                                if (shouldNotify)
                                {
                                    screenSaver.NotifyOledMouseActivity(screenName);
                                    lastActivityNotification[screenName] = DateTime.Now;
                                    LogHelper.Log($"Mouse activity detected on {screenName} - resetting timer");
                                }
                            }
                            break; // Mouse can only be on one screen at a time
                        }
                    }
                }

                // Check for global input (like typing) using GetLastInputInfo
                var lastInputInfo = new Win32Helper.LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
                lastInputInfo.dwTime = 0;

                if (Win32Helper.GetLastInputInfo(ref lastInputInfo))
                {
                    if (lastInputTick == 0)
                    {
                        lastInputTick = lastInputInfo.dwTime;
                    }
                    else if (lastInputInfo.dwTime != lastInputTick)
                    {
                        lastInputTick = lastInputInfo.dwTime;

                        // Only process this if the mouse didn't move much (otherwise it's already handled above)
                        if (!mouseMoved)
                        {
                            var foregroundWindow = Win32Helper.GetForegroundWindow();
                            if (foregroundWindow != IntPtr.Zero)
                            {
                                var activeScreen = Screen.FromHandle(foregroundWindow);
                                if (activeScreen != null)
                                {
                                    string activeScreenName = null;
                                    lock (formsLock)
                                    {
                                        foreach (var kvp in oledForms)
                                        {
                                            if (kvp.Value != null && !kvp.Value.IsDisposed && 
                                                kvp.Value.Bounds.Contains(activeScreen.Bounds.Location))
                                            {
                                                activeScreenName = kvp.Key;
                                                break;
                                            }
                                        }
                                    }

                                    if (activeScreenName != null)
                                    {
                                        var shouldNotify = true;
                                        if (lastActivityNotification.ContainsKey(activeScreenName))
                                        {
                                            var timeSinceLastNotification = (DateTime.Now - lastActivityNotification[activeScreenName]).TotalMilliseconds;
                                            if (timeSinceLastNotification < ACTIVITY_NOTIFICATION_COOLDOWN_MS)
                                            {
                                                shouldNotify = false;
                                            }
                                        }

                                        if (shouldNotify)
                                        {
                                            screenSaver.NotifyOledMouseActivity(activeScreenName);
                                            lastActivityNotification[activeScreenName] = DateTime.Now;
                                            LogHelper.Log($"Global input detected on {activeScreenName} - resetting timer");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Manage cursor visibility: hide only when mouse is on a fully black screen
                // Only change cursor state when it actually changes to avoid counter issues
                if (mouseOnFullyBlackScreen && !cursorCurrentlyHidden)
                {
                    // Jiggle cursor by 1 pixel to force Windows to apply the OLEDForm's blank cursor
                    var p = Cursor.Position;
                    Cursor.Position = new Point(p.X + 1, p.Y);
                    Cursor.Position = p;
                    lastMousePosition = p; // Update so this artificial jiggle doesn't trigger wake-up

                    // Hide cursor aggressively
                    Win32Helper.HideCursor();
                    cursorCurrentlyHidden = true;
                    LogHelper.Log($"Hiding cursor - mouse on fully black screen: {mouseScreenName}");
                }
                else if (!mouseOnFullyBlackScreen && cursorCurrentlyHidden)
                {
                    // Show cursor
                    Win32Helper.ShowCursor();
                    cursorCurrentlyHidden = false;
                    LogHelper.Log($"Showing cursor - mouse not on fully black screen");
                }
                else if (mouseOnFullyBlackScreen && cursorCurrentlyHidden)
                {
                    // Continuously hide cursor while on fully black screen (in case something else shows it)
                    // Call multiple times to ensure it stays hidden (Windows may reset it)
                    Win32Helper.HideCursor();
                    Win32Helper.HideCursor(); // Extra call for reliability
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in MouseActivityTimer_Tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes OLED forms based on current configuration and screen availability
        /// Thread-safe: ensures all UI operations happen on the UI thread
        /// </summary>
        private void RefreshOLEDForms()
        {
            // Ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshOLEDForms));
                return;
            }

            lock (formsLock)
            {
                try
                {
                    var screenNames = RegistryHelper.LoadScreenNames();
                    var currentScreenNames = new HashSet<string>(oledForms.Keys);
                    var targetScreenNames = new HashSet<string>(screenNames);

                    // Remove forms for screens that are no longer configured
                    var toRemove = new List<string>();
                    foreach (var screenName in currentScreenNames)
                    {
                        if (!targetScreenNames.Contains(screenName))
                        {
                            toRemove.Add(screenName);
                        }
                    }
                    foreach (var screenName in toRemove)
                    {
                        if (oledForms.TryGetValue(screenName, out var form))
                        {
                            if (form != null && !form.IsDisposed)
                            {
                                if (form.InvokeRequired)
                                {
                                    form.Invoke(new Action(() => { form.Hide(); form.Close(); form.Dispose(); }));
                                }
                                else
                                {
                                    form.Hide();
                                    form.Close();
                                    form.Dispose();
                                }
                            }
                            oledForms.Remove(screenName);
                            fullyBlackScreens.Remove(screenName);
                            lastActivityNotification.Remove(screenName);
                            LogHelper.Log($"Removed form for screen: {screenName}");
                        }
                    }

                    // Update or create forms for configured screens
                    foreach (var screenName in screenNames)
                    {
                        var screen = ScreenHelper.FindScreen(screenName);
                        if (screen != null)
                        {
                            if (oledForms.TryGetValue(screenName, out var existingForm))
                            {
                                // Update existing form position and size if screen changed
                                if (existingForm != null && !existingForm.IsDisposed)
                                {
                                    UpdateFormForScreen(existingForm, screen);
                                }
                                else
                                {
                                    // Form was disposed, create new one
                                    oledForms[screenName] = CreateFormForScreen(screenName, screen);
                                }
                            }
                            else
                            {
                                // Create new form
                                oledForms[screenName] = CreateFormForScreen(screenName, screen);
                            }
                        }
                        else
                        {
                            LogHelper.Log($"Screen not found: {screenName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Error in RefreshOLEDForms: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a form for a specific screen
        /// Thread-safe: ensures form is created on UI thread
        /// </summary>
        private OLEDForm CreateFormForScreen(string screenName, Screen screen)
        {
            // Ensure we're on the UI thread for form creation
            if (InvokeRequired)
            {
                OLEDForm result = null;
                Invoke(new Action(() =>
                {
                    result = CreateFormForScreenInternal(screenName, screen);
                }));
                return result;
            }

            return CreateFormForScreenInternal(screenName, screen);
        }

        /// <summary>
        /// Internal method that actually creates the form (must be called on UI thread)
        /// </summary>
        private OLEDForm CreateFormForScreenInternal(string screenName, Screen screen)
        {
            try
            {
                var form = new OLEDForm
                {
                    BackColor = Color.Black,
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Normal, // Use Normal instead of Maximized for better control
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    ShowInTaskbar = false,
                    Text = $"OLED Screen Saver - {screenName}",
                    Opacity = 0.0, // Start invisible - will be animated when shown
                    Cursor = new Cursor(Win32Helper.GetBlankCursor())
                };

                UpdateFormForScreen(form, screen);

                // Initially hide the form - it will be shown when needed
                form.Hide();

                LogHelper.Log($"Created form for screen: {screenName} at {form.Bounds}");
                return form;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in CreateFormForScreenInternal for {screenName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates form position and size to match screen bounds
        /// Thread-safe: ensures all UI operations happen on the UI thread
        /// </summary>
        private void UpdateFormForScreen(OLEDForm form, Screen screen)
        {
            if (form == null || form.IsDisposed) return;

            // Ensure we're on the UI thread
            if (form.InvokeRequired)
            {
                form.Invoke(new Action<OLEDForm, Screen>(UpdateFormForScreen), form, screen);
                return;
            }

            try
            {
                // Use actual screen bounds - this fixes the positioning issue
                // Ensure form covers the entire screen
                form.Bounds = screen.Bounds;
                form.WindowState = FormWindowState.Normal; // Normal state for precise control
                form.Size = screen.Bounds.Size;
                form.Location = screen.Bounds.Location;
                LogHelper.Log($"Updated form bounds to screen: {screen.Bounds} (Location: {screen.Bounds.Location}, Size: {screen.Bounds.Size})");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in UpdateFormForScreen: {ex.Message}");
            }
        }

        public void HideFormCallback(string screenName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(HideFormCallback), screenName);
                return;
            }

            try
            {
                LogHelper.Log($"HideFormCallback for screen: {screenName}");

                // Stop any animation for this screen
                lock (formsLock)
                {
                    if (opacityAnimations.TryGetValue(screenName, out var animationTimer))
                    {
                        if (animationTimer != null)
                        {
                            animationTimer.Stop();
                            animationTimer.Dispose();
                        }
                        opacityAnimations.Remove(screenName);
                    }
                }

                // Hide the specific OLED form for this screen
                lock (formsLock)
                {
                    if (oledForms.TryGetValue(screenName, out var form))
                    {
                        if (form != null && !form.IsDisposed)
                        {
                            var duration = RegistryHelper.LoadAnimationDuration();
                            AnimateOpacity(screenName, form, form.Opacity, 0.0, duration);
                        }
                    }
                    // Mark screen as not fully black
                    fullyBlackScreens[screenName] = false;
                }

                // Show cursor when screen is no longer dimmed
                if (cursorCurrentlyHidden)
                {
                    Win32Helper.ShowCursor();
                    cursorCurrentlyHidden = false;
                    LogHelper.Log($"Showing cursor - screen {screenName} no longer dimmed");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in HideFormCallback for {screenName}: {ex.Message}");
            }
        }

        public void ShowFirstStageCallback(string screenName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowFirstStageCallback), screenName);
                return;
            }

            try
            {
                LogHelper.Log($"ShowFirstStageCallback for screen: {screenName}");

                // Refresh forms in case screen configuration changed
                RefreshOLEDForms();

                // Show the specific OLED form at 50% opacity (first dim stage) with smooth animation
                lock (formsLock)
                {
                    if (oledForms.TryGetValue(screenName, out var form))
                    {
                        if (form != null && !form.IsDisposed)
                        {
                            // Ensure form is positioned correctly
                            var screen = ScreenHelper.FindScreen(screenName);
                            if (screen != null)
                            {
                                UpdateFormForScreen(form, screen);
                            }

                            // Set initial opacity to 0 before showing
                            form.Opacity = 0.0;
                            // Show window without activating (doesn't steal focus)
                            Win32Helper.ShowWindowNoActivate(form.Handle);
                            form.TopMost = true;
                            // Don't call BringToFront() or Activate() - they steal focus

                            // Animate opacity using configured percentage and duration
                            var targetOpacity = RegistryHelper.LoadDimPercentage() / 100.0;
                            var animationDuration = RegistryHelper.LoadAnimationDuration();
                            AnimateOpacity(screenName, form, 0.0, targetOpacity, animationDuration);

                            // Mark screen as not fully black (first stage = 50% dim)
                            lock (formsLock)
                            {
                                fullyBlackScreens[screenName] = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in ShowFirstStageCallback for {screenName}: {ex.Message}");
            }
        }

        public void ShowSecondStageCallback(string screenName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowSecondStageCallback), screenName);
                return;
            }

            try
            {
                LogHelper.Log($"ShowSecondStageCallback for screen: {screenName}");

                // Update opacity to full (second stage) for the specific screen with smooth animation
                lock (formsLock)
                {
                    if (oledForms.TryGetValue(screenName, out var form))
                    {
                        if (form != null && !form.IsDisposed)
                        {
                            if (!form.Visible)
                            {
                                // Show window without activating (doesn't steal focus)
                                Win32Helper.ShowWindowNoActivate(form.Handle);
                            }
                            form.TopMost = true;
                            // Don't call BringToFront() or Activate() - they steal focus

                            // Animate opacity from current to 1.0
                            var currentOpacity = form.Opacity;
                            var animationDuration = RegistryHelper.LoadAnimationDuration();
                            AnimateOpacity(screenName, form, currentOpacity, 1.0, animationDuration);

                            // Mark screen as fully black (second stage = 100% dim)
                            lock (formsLock)
                            {
                                fullyBlackScreens[screenName] = true;
                            }

                            // Hide cursor if mouse is on this screen
                            var cursorPosition = Cursor.Position;
                            var screen = ScreenHelper.FindScreen(screenName);
                            if (screen != null && screen.Bounds.Contains(cursorPosition))
                            {
                                Win32Helper.HideCursor();
                                cursorCurrentlyHidden = true;
                                LogHelper.Log($"Hiding cursor immediately - mouse on fully black screen: {screenName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in ShowSecondStageCallback for {screenName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Animates form opacity from start to end with ease-in easing function
        /// </summary>
        private void AnimateOpacity(string screenName, OLEDForm form, double startOpacity, double endOpacity, int durationMs)
        {
            if (form == null || form.IsDisposed) return;

            // Stop any existing animation for this screen
            lock (formsLock)
            {
                if (opacityAnimations.TryGetValue(screenName, out var existingTimer))
                {
                    if (existingTimer != null)
                    {
                        existingTimer.Stop();
                        existingTimer.Dispose();
                    }
                    opacityAnimations.Remove(screenName);
                }
            }

            // Ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(new Action<string, OLEDForm, double, double, int>(AnimateOpacity), screenName, form, startOpacity, endOpacity, durationMs);
                return;
            }

            try
            {
                // Set initial opacity before animation starts
                if (form.InvokeRequired)
                {
                    form.Invoke(new Action(() =>
                    {
                        if (!form.IsDisposed)
                        {
                            form.Opacity = startOpacity;
                        }
                    }));
                }
                else
                {
                    form.Opacity = startOpacity;
                }

                // Skip animation if duration is very short (e.g. 0 ms set in config)
                if (durationMs <= 50)
                {
                    if (form.InvokeRequired)
                    {
                        form.Invoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                            {
                                form.Opacity = endOpacity;
                                if (endOpacity <= 0.01) { form.Hide(); form.SendToBack(); form.TopMost = false; }
                            }
                        }));
                    }
                    else
                    {
                        form.Opacity = endOpacity;
                        if (endOpacity <= 0.01) { form.Hide(); form.SendToBack(); form.TopMost = false; }
                    }
                    return;
                }

                // Use a simpler, more reliable animation approach
                int targetSteps = 20; // Try up to 20 steps
                var stepInterval = Math.Max(16, durationMs / targetSteps); // At least 16ms per step (60fps)
                var steps = Math.Max(1, durationMs / stepInterval);
                var opacityRange = endOpacity - startOpacity;
                var stepSize = opacityRange / steps;
                var currentStep = 0;

                var animationTimer = new Timer
                {
                    Interval = stepInterval,
                    Enabled = true
                };

                animationTimer.Tick += (sender, e) =>
                {
                    try
                    {
                        if (form == null || form.IsDisposed)
                        {
                            animationTimer.Stop();
                            animationTimer.Dispose();
                            lock (formsLock)
                            {
                                opacityAnimations.Remove(screenName);
                            }
                            return;
                        }

                        currentStep++;
                        var progress = (double)currentStep / steps;

                        // Ease-in function: progress^2 (quadratic ease-in)
                        var easedProgress = progress * progress;
                        var currentOpacity = startOpacity + (opacityRange * easedProgress);

                        if (form.InvokeRequired)
                        {
                            form.Invoke(new Action(() =>
                            {
                                if (!form.IsDisposed)
                                {
                                    form.Opacity = Math.Min(1.0, Math.Max(0.0, currentOpacity));
                                }
                            }));
                        }
                        else
                        {
                            form.Opacity = Math.Min(1.0, Math.Max(0.0, currentOpacity));
                        }

                        if (currentStep < steps) return;

                        if (form.InvokeRequired)
                        {
                            form.Invoke(new Action(() =>
                            {
                                if (!form.IsDisposed)
                                {
                                    form.Opacity = endOpacity;
                                    if (endOpacity <= 0.01) { form.Hide(); form.SendToBack(); form.TopMost = false; }
                                }
                            }));
                        }
                        else
                        {
                            form.Opacity = endOpacity;
                            if (endOpacity <= 0.01) { form.Hide(); form.SendToBack(); form.TopMost = false; }
                        }

                        animationTimer.Stop();
                        animationTimer.Dispose();
                        lock (formsLock)
                        {
                            opacityAnimations.Remove(screenName);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log($"Error in opacity animation tick: {ex.Message}");
                        animationTimer.Stop();
                        animationTimer.Dispose();
                        lock (formsLock)
                        {
                            opacityAnimations.Remove(screenName);
                        }
                    }
                };

                // Store timer and start animation
                lock (formsLock)
                {
                    opacityAnimations[screenName] = animationTimer;
                }

                // Start the animation timer
                animationTimer.Start();
                LogHelper.Log($"Started opacity animation for {screenName}: {startOpacity} -> {endOpacity} over {durationMs}ms (steps={steps}, interval={stepInterval}ms)");

                // Force an immediate update to ensure form is visible
                if (form.InvokeRequired)
                {
                    form.Invoke(new Action(() =>
                    {
                        if (form.IsDisposed) return;
                        form.Opacity = startOpacity;
                    }));
                }
                else
                {
                    form.Opacity = startOpacity;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error starting opacity animation: {ex.Message}");
            }
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void UpdatePauseMenu(bool isPaused)
        {
            menuItem1.Checked = isPaused;
            if (!isPaused) screenSaver.ResumeScreensaver();
        }

        private void MenuItem1_Click(object Sender, EventArgs e)
        {
            UpdatePauseMenu(false);
            Console.WriteLine("Resumed.");
        }

        private ConfigForm configFormInstance = null;

        private void MenuItem2_Click(object Sender, EventArgs e)
        {
            try
            {
                // Ensure form handle is created before using Invoke
                if (!IsHandleCreated)
                {
                    CreateHandle();
                }

                // Ensure we're on the UI thread
                if (InvokeRequired)
                {
                    BeginInvoke(new EventHandler(MenuItem2_Click), Sender, e);
                    return;
                }

                if (configFormInstance != null && !configFormInstance.IsDisposed)
                {
                    if (configFormInstance.WindowState == FormWindowState.Minimized)
                    {
                        configFormInstance.WindowState = FormWindowState.Normal;
                    }
                    configFormInstance.BringToFront();
                    configFormInstance.Activate();
                    return;
                }

                // Open config form directly on UI thread
                configFormInstance = new ConfigForm(screenSaver);
                var dialogResult = configFormInstance.ShowDialog(this);
                
                // Set to null now that it relates to a closed dialog
                configFormInstance = null;

                if (dialogResult != DialogResult.Yes) return;
                screenSaver.UpdateTimeout();
                screenSaver.UpdatePollRate();
                screenSaver.UpdateSecondStageTimeout();
                screenSaver.UpdateScreenNames();
                screenSaver.UpdateDimEnabled();
                if (mouseActivityTimer != null)
                {
                    mouseActivityTimer.Interval = RegistryHelper.LoadPollRate();
                }
                // Refresh forms when configuration changes
                RefreshOLEDForms();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in menuItem2_Click: {ex.Message}");
                MessageBox.Show($"Error opening configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuItem3_Click(object Sender, EventArgs e)
        {
            screenSaver.PauseScreensaver();
            Console.WriteLine("Exiting...");

            // Dispose all OLED forms
            lock (formsLock)
            {
                foreach (var kvp in oledForms)
                {
                    var form = kvp.Value;
                    if (form != null && !form.IsDisposed)
                    {
                        form.Hide();
                        form.Close();
                        form.Dispose();
                    }
                }
                oledForms.Clear();
                fullyBlackScreens.Clear();
                lastActivityNotification.Clear();
            }

            // Show cursor before exit
            Win32Helper.ShowCursor();
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up screen saver
            if (screenSaver != null)
            {
                screenSaver.PauseScreensaver();
                screenSaver.Dispose();
            }

            // Clean up forms on application exit
            lock (formsLock)
            {
                foreach (var kvp in oledForms)
                {
                    var form = kvp.Value;
                    if (form != null && !form.IsDisposed)
                    {
                        form.Hide();
                        form.Close();
                        form.Dispose();
                    }
                }
                oledForms.Clear();
                fullyBlackScreens.Clear();
            }

            // Show cursor on exit
            Win32Helper.ShowCursor();
            base.OnFormClosing(e);
        }

    }

    // Custom form class that hides from Alt+Tab and handles screen positioning properly
    public class OLEDForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Hide from Alt+Tab switcher
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
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
