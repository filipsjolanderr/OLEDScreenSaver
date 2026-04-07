using System;
using System.Collections.Generic;
using System.Linq;

namespace OLEDScreenSaver
{
    public class ScreenSaverManager : IScreenSaverManager
    {
        private readonly IConfigurationRepository _configRepository;
        private readonly IUserActivityMonitor _userActivityMonitor;
        private readonly ILogger _logger;
        private readonly IScreenService _screenService;
        private System.Timers.Timer _screenSaverTimer;

        private bool _paused = false;
        private DateTime? _pauseEndTime = null;

        private uint _firstThresholdTime = 0;
        private uint _secondStageDelay = 0;
        private uint _pollrate = 0;

        private readonly object _stateLock = new object();

        // Per-screen tracking: screen name -> last mouse activity time
        private Dictionary<string, DateTime> _lastActivity = new Dictionary<string, DateTime>();
        // Per-screen display state: screen name -> is displayed
        private Dictionary<string, bool> _displayedScreens = new Dictionary<string, bool>();
        // Per-screen second stage state: screen name -> is in second stage
        private Dictionary<string, bool> _secondStageScreens = new Dictionary<string, bool>();

        private List<string> _cachedScreenNames = new List<string>();
        private bool _cachedDimEnabled = true;

        public event EventHandler<ScreenEventArgs> OnFirstStageDim;
        public event EventHandler<ScreenEventArgs> OnSecondStageDim;
        public event EventHandler<ScreenEventArgs> OnWake;

        public ScreenSaverManager(
            IConfigurationRepository configRepository, 
            IUserActivityMonitor userActivityMonitor,
            ILogger logger,
            IScreenService screenService)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _userActivityMonitor = userActivityMonitor ?? throw new ArgumentNullException(nameof(userActivityMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _screenService = screenService ?? throw new ArgumentNullException(nameof(screenService));

            _userActivityMonitor.OnUserActivity += HandleUserActivity;

            ReloadConfiguration();
        }

        public void ReloadConfiguration()
        {
            lock (_stateLock)
            {
                _firstThresholdTime = (uint)(_configRepository.LoadTimeout() * 60.0 * 1000.0);
                _secondStageDelay = (uint)(_configRepository.LoadSecondStageTimeout() * 60.0 * 1000.0);
                _pollrate = (uint)_configRepository.LoadPollRate();
                _cachedScreenNames = _configRepository.LoadScreenNames();
                _cachedDimEnabled = _configRepository.LoadDimEnabled();

                foreach (var screenName in _cachedScreenNames)
                {
                    if (!_lastActivity.ContainsKey(screenName))
                    {
                        _lastActivity[screenName] = DateTime.Now;
                        _displayedScreens[screenName] = false;
                        _secondStageScreens[screenName] = false;
                    }
                }

                // If timer exists, update its interval
                if (_screenSaverTimer != null && _screenSaverTimer.Enabled)
                {
                    _screenSaverTimer.Interval = _pollrate > 0 ? _pollrate : 500;
                }
            }
        }

        private void HandleUserActivity(object sender, UserActivityEventArgs e)
        {
            lock (_stateLock)
            {
                var screenName = e.ScreenName;
                if (!_lastActivity.ContainsKey(screenName))
                {
                    _lastActivity[screenName] = DateTime.Now;
                    _displayedScreens[screenName] = false;
                    _secondStageScreens[screenName] = false;
                }
                else
                {
                    _lastActivity[screenName] = DateTime.Now;
                    if (_displayedScreens.TryGetValue(screenName, out bool isDisplayed) && isDisplayed)
                    {
                        _displayedScreens[screenName] = false;
                        _secondStageScreens[screenName] = false;
                        WakeScreen(screenName);
                    }
                }
            }
        }

        public void Launch()
        {
            lock (_stateLock)
            {
                if (_screenSaverTimer != null)
                {
                    _screenSaverTimer.Stop();
                    _screenSaverTimer.Dispose();
                }

                _screenSaverTimer = new System.Timers.Timer(_pollrate > 0 ? _pollrate : 500);
                _screenSaverTimer.Elapsed += Tick;
                _screenSaverTimer.AutoReset = true;
                _screenSaverTimer.Enabled = true;

                _userActivityMonitor.Start();

                _logger.Log($"ScreenSaverManager timer launched with poll rate: {_pollrate}ms");
            }
        }

        private void Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                lock (_stateLock)
                {
                    if (_paused)
                    {
                        if (_pauseEndTime.HasValue && DateTime.Now >= _pauseEndTime.Value)
                        {
                            _logger.Log("Auto-resuming screensaver after pause duration expired.");
                            _paused = false;
                            _pauseEndTime = null;
                            foreach (var name in _cachedScreenNames)
                            {
                                _lastActivity[name] = DateTime.Now;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    // Assure all cached screens are tracked
                    foreach (var screenName in _cachedScreenNames)
                    {
                        if (!_lastActivity.ContainsKey(screenName))
                        {
                            _lastActivity[screenName] = DateTime.Now;
                            _displayedScreens[screenName] = false;
                            _secondStageScreens[screenName] = false;
                        }
                    }

                    // Clean up tracking for removed screens
                    var screensToRemove = _lastActivity.Keys.Where(k => !_cachedScreenNames.Contains(k)).ToList();
                    foreach (var screenName in screensToRemove)
                    {
                        _lastActivity.Remove(screenName);
                        _displayedScreens.Remove(screenName);
                        _secondStageScreens.Remove(screenName);
                    }

                    if (_paused) return; // double check after possible auto-resume

                    // Check conditions per screen
                    foreach (var screenName in _cachedScreenNames)
                    {
                        if (!_lastActivity.ContainsKey(screenName)) continue;

                        var timeSinceActivity = (uint)(DateTime.Now - _lastActivity[screenName]).TotalMilliseconds;
                        
                        var shouldDisplayFirstStage = _cachedDimEnabled && timeSinceActivity > _firstThresholdTime && timeSinceActivity <= (_firstThresholdTime + _secondStageDelay);
                        var shouldDisplaySecondStage = _cachedDimEnabled ? timeSinceActivity > (_firstThresholdTime + _secondStageDelay) : timeSinceActivity > _secondStageDelay;
                        
                        var isDisplayed = _displayedScreens.ContainsKey(screenName) && _displayedScreens[screenName];
                        var isSecondStage = _secondStageScreens.ContainsKey(screenName) && _secondStageScreens[screenName];

                        if (shouldDisplayFirstStage && !isDisplayed && !isSecondStage)
                        {
                            _displayedScreens[screenName] = true;
                            _secondStageScreens[screenName] = false;
                            DimFirstStage(screenName);
                        }
                        else if (!shouldDisplayFirstStage && !shouldDisplaySecondStage && isDisplayed)
                        {
                            _displayedScreens[screenName] = false;
                            _secondStageScreens[screenName] = false;
                            WakeScreen(screenName);
                        }
                        else if (shouldDisplaySecondStage && !isSecondStage)
                        {
                            _displayedScreens[screenName] = true;
                            _secondStageScreens[screenName] = true;
                            DimSecondStage(screenName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error in ScreenSaverManager Tick: {ex.Message}");
            }
        }

        private void DimFirstStage(string screenName)
        {
            _logger.Log($"ScreenSaverManager: First dim stage for screen: {screenName}");
            OnFirstStageDim?.Invoke(this, new ScreenEventArgs(screenName));
        }

        private void WakeScreen(string screenName)
        {
            _logger.Log($"ScreenSaverManager: Wake screen: {screenName}");
            OnWake?.Invoke(this, new ScreenEventArgs(screenName));
        }

        private void DimSecondStage(string screenName)
        {
            _logger.Log($"ScreenSaverManager: Second dim stage for screen: {screenName}");
            OnSecondStageDim?.Invoke(this, new ScreenEventArgs(screenName));
        }

        public void Pause(int? minutes = null)
        {
            lock (_stateLock)
            {
                _paused = true;
                _pauseEndTime = minutes.HasValue ? (DateTime?)DateTime.Now.AddMinutes(minutes.Value) : null;
                
                // Hide all displayed screens
                foreach (var screenName in _displayedScreens.Keys.ToList())
                {
                    if (_displayedScreens[screenName])
                    {
                        _displayedScreens[screenName] = false;
                        _secondStageScreens[screenName] = false;
                        WakeScreen(screenName);
                    }
                }
                
                if (_screenSaverTimer != null) _screenSaverTimer.Stop();
                _logger.Log("Screensaver paused");
            }
        }

        public void Resume()
        {
            lock (_stateLock)
            {
                _paused = false;
                _pauseEndTime = null;
                
                // Reset activity time so it doesn't immediately dim
                foreach (var screenName in _cachedScreenNames)
                {
                    _lastActivity[screenName] = DateTime.Now;
                }

                if (_screenSaverTimer != null) _screenSaverTimer.Start();
                _logger.Log("Screensaver resumed");
            }
        }

        public void ToggleScreensaver()
        {
            lock (_stateLock)
            {
                bool allDisplayed = _secondStageScreens.Count > 0 && _cachedScreenNames.All(k => _secondStageScreens.ContainsKey(k) && _secondStageScreens[k]);
                
                if (allDisplayed)
                {
                    // Wake all
                    foreach (var scr in _cachedScreenNames)
                    {
                        _lastActivity[scr] = DateTime.Now;
                        _displayedScreens[scr] = false;
                        _secondStageScreens[scr] = false;
                        WakeScreen(scr);
                    }
                }
                else
                {
                    // Force dim all
                    foreach (var scr in _cachedScreenNames)
                    {
                        _displayedScreens[scr] = true;
                        _secondStageScreens[scr] = true;
                        _lastActivity[scr] = DateTime.Now.AddDays(-1); // Force old timestamp
                        DimSecondStage(scr);
                    }
                }
                
                _userActivityMonitor.IgnoreInput(TimeSpan.FromSeconds(1));
            }
        }

        public void Dispose()
        {
            lock (_stateLock)
            {
                if (_screenSaverTimer != null)
                {
                    _screenSaverTimer.Stop();
                    _screenSaverTimer.Dispose();
                    _screenSaverTimer = null;
                }

                if (_userActivityMonitor != null)
                {
                    _userActivityMonitor.OnUserActivity -= HandleUserActivity;
                    _userActivityMonitor.Stop();
                }
            }
        }
    }
}
