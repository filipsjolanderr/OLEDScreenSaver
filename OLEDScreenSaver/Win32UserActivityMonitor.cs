using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace OLEDScreenSaver
{
    public class Win32UserActivityMonitor : IUserActivityMonitor, IDisposable
    {
        private readonly IConfigurationRepository _configRepository;
        private readonly ILogger _logger;
        private readonly IScreenService _screenService;
        
        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private delegate IntPtr HookDelegate(int nCode, IntPtr wParam, IntPtr lParam);
        private HookDelegate _mouseHookDelegate;
        private HookDelegate _keyboardHookDelegate;
        private Point _lastMousePosition;
        private DateTime _ignoreInputUntil = DateTime.MinValue;
        private readonly object _lock = new object();
        
        private Timer _fallbackTimer;

        private DateTime _lastActivityProcessTime = DateTime.MinValue;
        private List<ScreenInfoCache> _cachedScreens = new List<ScreenInfoCache>();
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        private class ScreenInfoCache
        {
            public string Name { get; set; }
            public Rectangle Bounds { get; set; }
        }

        private List<ScreenInfoCache> GetCachedScreens()
        {
            lock (_lock)
            {
                if ((DateTime.Now - _lastCacheUpdate).TotalSeconds > 5)
                {
                    var newCache = new List<ScreenInfoCache>();
                    var screens = _configRepository.LoadScreenNames();
                    foreach (var name in screens)
                    {
                        var screen = _screenService.FindScreenByName(name);
                        if (screen != null)
                        {
                            newCache.Add(new ScreenInfoCache { Name = name, Bounds = screen.Bounds });
                        }
                    }
                    _cachedScreens = newCache;
                    _lastCacheUpdate = DateTime.Now;
                }
                return _cachedScreens;
            }
        }

        public event EventHandler<UserActivityEventArgs> OnUserActivity;

        public Win32UserActivityMonitor(IConfigurationRepository configRepository, ILogger logger, IScreenService screenService)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _screenService = screenService ?? throw new ArgumentNullException(nameof(screenService));
        }

        public void Start()
        {
            InstallHooks();
            
            _fallbackTimer = new Timer { Interval = Math.Max(100, _configRepository.LoadPollRate()) };
            _fallbackTimer.Tick += FallbackTimer_Tick;
            _fallbackTimer.Start();
        }

        private void InstallHooks()
        {
            try
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                using (var module = process.MainModule)
                {
                    var hModule = NativeMethods.GetModuleHandle(module.ModuleName);
                    
                    _mouseHookDelegate = MouseHookCallback;
                    _keyboardHookDelegate = KeyboardHookCallback;

                    _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseHookDelegate, hModule, 0);
                    _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardHookDelegate, hModule, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to install Win32 hooks", ex);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_MOUSEMOVE)
            {
                var hookStruct = (NativeMethods.MSLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));
                bool isInjected = (hookStruct.flags & NativeMethods.LLMHF_INJECTED) != 0 || 
                                  (hookStruct.flags & NativeMethods.LLMHF_LOWER_IL_INJECTED) != 0;

                if (!isInjected)
                {
                    NotifyActivity();
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
            {
                NotifyActivity();
            }
            return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private void NotifyActivity()
        {
            lock (_lock)
            {
                if (DateTime.Now < _ignoreInputUntil) return;
                
                // Throttle activity processing to avoid overwhelming the system
                if ((DateTime.Now - _lastActivityProcessTime).TotalMilliseconds < 50) return;
                _lastActivityProcessTime = DateTime.Now;
            }

            var pos = Cursor.Position;
            
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var screens = GetCachedScreens();
                    foreach (var s in screens)
                    {
                        if (s.Bounds.Contains(pos))
                        {
                            OnUserActivity?.Invoke(this, new UserActivityEventArgs(s.Name, true));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error checking activity", ex);
                }
            });
        }

        private void FallbackTimer_Tick(object sender, EventArgs e)
        {
            var lii = new NativeMethods.LASTINPUTINFO { cbSize = (uint)NativeMethods.LASTINPUTINFO.SizeOf };
            if (NativeMethods.GetLastInputInfo(ref lii))
            {
                var currentMousePosition = Cursor.Position;
                if (currentMousePosition != _lastMousePosition)
                {
                    _lastMousePosition = currentMousePosition;
                    NotifyActivity();
                }
            }
        }

        public void IgnoreInput(TimeSpan duration)
        {
            lock (_lock)
            {
                _ignoreInputUntil = DateTime.Now.Add(duration);
            }
        }

        public void Stop()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            _fallbackTimer?.Stop();
        }

        public void Dispose()
        {
            Stop();
            _fallbackTimer?.Dispose();
        }
    }
}
