using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public class OledFormManager : IDisposable
    {
        private readonly IConfigurationRepository _configRepository;
        private readonly IScreenSaverManager _screenSaverManager;
        private readonly ILogger _logger;
        private readonly IScreenService _screenService;
        private readonly ICursorService _cursorService;
        private readonly IWindowManager _windowManager;
        
        private readonly Dictionary<string, OLEDForm> _oledForms = new Dictionary<string, OLEDForm>();
        private readonly Dictionary<string, Timer> _opacityAnimations = new Dictionary<string, Timer>();
        private readonly object _formsLock = new object();
        
        private bool _cursorCurrentlyHidden = false;

        public OledFormManager(
            IConfigurationRepository configRepository, 
            IScreenSaverManager screenSaverManager,
            ILogger logger,
            IScreenService screenService,
            ICursorService cursorService,
            IWindowManager windowManager)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _screenSaverManager = screenSaverManager ?? throw new ArgumentNullException(nameof(screenSaverManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _screenService = screenService ?? throw new ArgumentNullException(nameof(screenService));
            _cursorService = cursorService ?? throw new ArgumentNullException(nameof(cursorService));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            _screenSaverManager.OnFirstStageDim += HandleFirstStageDim;
            _screenSaverManager.OnSecondStageDim += HandleSecondStageDim;
            _screenSaverManager.OnWake += HandleWake;

            RefreshOLEDForms();
        }

        public void RefreshOLEDForms()
        {
            lock (_formsLock)
            {
                var targetScreenNames = _configRepository.LoadScreenNames();
                var currentScreenNames = _oledForms.Keys.ToList();

                var toRemove = currentScreenNames.Where(n => !targetScreenNames.Contains(n)).ToList();
                foreach (var screenName in toRemove)
                {
                    if (_oledForms.TryGetValue(screenName, out var form))
                    {
                        if (form != null && !form.IsDisposed)
                        {
                            form.Hide();
                            form.Close();
                            form.Dispose();
                        }
                        _oledForms.Remove(screenName);
                    }
                }

                foreach (var screenName in targetScreenNames)
                {
                    var screen = _screenService.FindScreenByName(screenName);
                    if (screen != null)
                    {
                        if (_oledForms.TryGetValue(screenName, out var existingForm))
                        {
                            if (existingForm != null && !existingForm.IsDisposed)
                            {
                                UpdateFormForScreen(existingForm, screen);
                            }
                            else
                            {
                                _oledForms[screenName] = CreateFormForScreenInternal(screenName, screen);
                            }
                        }
                        else
                        {
                            _oledForms[screenName] = CreateFormForScreenInternal(screenName, screen);
                        }
                    }
                }
            }
        }

        private OLEDForm CreateFormForScreenInternal(string screenName, Screen screen)
        {
            var form = new OLEDForm
            {
                BackColor = Color.Black,
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Normal,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                Text = $"OLED Screen Saver - {screenName}",
                Opacity = 0.0,
                Cursor = new Cursor(_cursorService.GetBlankCursor())
            };

            UpdateFormForScreen(form, screen);
            var _ = form.Handle; // Force handle creation on the UI thread to make InvokeRequired correct
            form.Hide();

            return form;
        }

        private void UpdateFormForScreen(OLEDForm form, Screen screen)
        {
            if (form == null || form.IsDisposed) return;
            form.Bounds = screen.Bounds;
            form.WindowState = FormWindowState.Normal;
            form.Size = screen.Bounds.Size;
            form.Location = screen.Bounds.Location;
        }

        private void SafeInvoke(OLEDForm form, Action action)
        {
            if (form != null && !form.IsDisposed)
            {
                if (form.InvokeRequired)
                {
                    form.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
        }

        private void HandleFirstStageDim(object sender, ScreenEventArgs e)
        {
            var screenName = e.ScreenName;
            lock (_formsLock)
            {
                if (_oledForms.TryGetValue(screenName, out var form) && form != null && !form.IsDisposed)
                {
                    SafeInvoke(form, () =>
                    {
                        if (!form.Visible)
                        {
                            form.Opacity = 0.0;
                            _windowManager.ShowNoActivate(form.Handle);
                        }
                        form.TopMost = true;

                        var targetOpacity = _configRepository.LoadDimPercentage() / 100.0;
                        var animationDuration = _configRepository.LoadAnimationDuration();
                        AnimateOpacity(screenName, form, form.Opacity, targetOpacity, animationDuration);
                    });
                }
            }
        }

        private void HandleSecondStageDim(object sender, ScreenEventArgs e)
        {
            var screenName = e.ScreenName;
            lock (_formsLock)
            {
                if (_oledForms.TryGetValue(screenName, out var form) && form != null && !form.IsDisposed)
                {
                    SafeInvoke(form, () =>
                    {
                        if (!form.Visible)
                        {
                            form.Opacity = 0.0;
                            _windowManager.ShowNoActivate(form.Handle);
                        }
                        form.TopMost = true;

                        var animationDuration = _configRepository.LoadAnimationDuration();
                        AnimateOpacity(screenName, form, form.Opacity, 1.0, animationDuration);
                        ManageCursorVisibility(screenName, hide: true);
                    });
                }
            }
        }

        private void HandleWake(object sender, ScreenEventArgs e)
        {
            var screenName = e.ScreenName;
            lock (_formsLock)
            {
                if (_oledForms.TryGetValue(screenName, out var form) && form != null && !form.IsDisposed)
                {
                    SafeInvoke(form, () =>
                    {
                        var animationDuration = _configRepository.LoadAnimationDuration();
                        AnimateOpacity(screenName, form, form.Opacity, 0.0, animationDuration);
                        ManageCursorVisibility(screenName, hide: false);
                    });
                }
            }
        }

        private void ManageCursorVisibility(string screenName, bool hide)
        {
            if (hide)
            {
                var cursorPosition = Cursor.Position;
                var screen = _screenService.FindScreenByName(screenName);
                if (screen != null && screen.Bounds.Contains(cursorPosition))
                {
                    if (!_cursorCurrentlyHidden)
                    {
                        // Jiggle cursor by 1 pixel to force Windows to apply the OLEDForm's blank cursor
                        var p = Cursor.Position;
                        Cursor.Position = new Point(p.X + 1, p.Y);
                        Cursor.Position = p;

                        _cursorService.Hide();
                        _cursorCurrentlyHidden = true;
                    }
                    else
                    {
                        // Periodically ensure it stays hidden
                        _cursorService.Hide();
                    }
                }
            }
            else
            {
                if (_cursorCurrentlyHidden)
                {
                    _cursorService.Show();
                    _cursorCurrentlyHidden = false;
                }
            }
        }

        private void AnimateOpacity(string screenName, OLEDForm form, double startOpacity, double endOpacity, int durationMs)
        {
            if (form == null || form.IsDisposed) return;

            lock (_formsLock)
            {
                if (_opacityAnimations.TryGetValue(screenName, out var existingTimer) && existingTimer != null)
                {
                    existingTimer.Stop();
                    existingTimer.Dispose();
                    _opacityAnimations.Remove(screenName);
                }
            }

            form.Opacity = startOpacity;

            if (durationMs <= 50)
            {
                form.Opacity = endOpacity;
                if (endOpacity <= 0.01)
                {
                    form.Hide();
                    form.SendToBack();
                    form.TopMost = false;
                }
                return;
            }

            int targetSteps = 20;
            var stepInterval = Math.Max(16, durationMs / targetSteps);
            var steps = Math.Max(1, durationMs / stepInterval);
            var opacityRange = endOpacity - startOpacity;
            var stepSize = opacityRange / steps;
            var currentStep = 0;

            var animationTimer = new Timer
            {
                Interval = stepInterval,
                Enabled = true
            };

            animationTimer.Tick += (sender, args) =>
            {
                if (form == null || form.IsDisposed)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    lock (_formsLock) { _opacityAnimations.Remove(screenName); }
                    return;
                }

                currentStep++;
                var progress = (double)currentStep / steps;

                // ease-in
                var easeProgress = progress * progress;
                // Avoid overshooting completely due to float math
                var nextOpacity = startOpacity + (opacityRange * (opacityRange > 0 ? easeProgress : progress));

                if (currentStep >= steps)
                {
                    form.Opacity = endOpacity;
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    lock (_formsLock) { _opacityAnimations.Remove(screenName); }

                    if (endOpacity <= 0.01)
                    {
                        form.Hide();
                        form.SendToBack();
                        form.TopMost = false;
                    }
                }
                else
                {
                    form.Opacity = nextOpacity;
                }
            };

            lock (_formsLock)
            {
                _opacityAnimations[screenName] = animationTimer;
            }
        }

        public void Dispose()
        {
            _screenSaverManager.OnFirstStageDim -= HandleFirstStageDim;
            _screenSaverManager.OnSecondStageDim -= HandleSecondStageDim;
            _screenSaverManager.OnWake -= HandleWake;

            lock (_formsLock)
            {
                foreach (var timer in _opacityAnimations.Values)
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                }
                _opacityAnimations.Clear();

                foreach (var form in _oledForms.Values)
                {
                    if (form != null && !form.IsDisposed)
                    {
                        form.Hide();
                        form.Close();
                        form.Dispose();
                    }
                }
                _oledForms.Clear();
            }
            
            if (_cursorCurrentlyHidden)
            {
                _cursorService.Show();
                _cursorCurrentlyHidden = false;
            }
        }
    }
}
