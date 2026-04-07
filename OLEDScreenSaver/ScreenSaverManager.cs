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

        private Dictionary<string, System.Timers.Timer> _timers = new Dictionary<string, System.Timers.Timer>();
        private System.Timers.Timer _unpauseTimer = null;

        private bool _paused = false;
        private DateTime? _pauseEndTime = null;

        private uint _firstThresholdTime = 0;
        private uint _secondStageDelay = 0;

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
                
                // Clean up removed screens
                var screensToRemove = _timers.Keys.Where(k => !_cachedScreenNames.Contains(k)).ToList();
                foreach (var key in screensToRemove)
                {
                    if (_timers.TryGetValue(key, out var timer))
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    _timers.Remove(key);
                    _lastActivity.Remove(key);
                    _displayedScreens.Remove(key);
                    _secondStageScreens.Remove(key);
                }

                foreach (var screenName in _cachedScreenNames)
                {
                    if (!_timers.ContainsKey(screenName))
                    {
                        var tmr = new System.Timers.Timer(GetInitialInterval());
                        tmr.Elapsed += (s, e) => ScreenTimerElapsed(screenName);
                        tmr.AutoReset = false;
                        _timers[screenName] = tmr;
                        
                        if (!_paused) tmr.Start();
                    }
                }
            }
        }

        private double GetInitialInterval()
        {
            double interval = _cachedDimEnabled ? _firstThresholdTime : _secondStageDelay;
            return interval > 0 ? interval : 500;
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
                    
                    if (!_timers.ContainsKey(screenName))
                    {
                        var tmr = new System.Timers.Timer(GetInitialInterval());
                        tmr.Elapsed += (s, ev) => ScreenTimerElapsed(screenName);
                        tmr.AutoReset = false;
                        _timers[screenName] = tmr;
                        if (!_paused) tmr.Start();
                    }
                }
                else
                {
                    _lastActivity[screenName] = DateTime.Now;
                    if (_displayedScreens.TryGetValue(screenName, out bool isDisplayed) && isDisplayed)
                    {
                        _displayedScreens[screenName] = false;
                        _secondStageScreens[screenName] = false;
                        WakeScreen(screenName);
                        
                        if (_timers.TryGetValue(screenName, out var timer))
                        {
                            timer.Stop();
                            timer.Interval = GetInitialInterval();
                            if (!_paused) timer.Start();
                        }
                    }
                }
            }
        }

        public void Launch()
        {
            lock (_stateLock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                _timers.Clear();

                foreach (var screenName in _cachedScreenNames)
                {
                    var timer = new System.Timers.Timer(GetInitialInterval());
                    timer.Elapsed += (s, e) => ScreenTimerElapsed(screenName);
                    timer.AutoReset = false;
                    _timers[screenName] = timer;
                    timer.Start();
                }

                _userActivityMonitor.Start();

                _logger.Log($"ScreenSaverManager launched with sliding expiration pattern");
            }
        }

        private void ScreenTimerElapsed(string screenName)
        {
            try
            {
                lock (_stateLock)
                {
                    if (_paused) return;

                    if (!_lastActivity.ContainsKey(screenName) || !_timers.TryGetValue(screenName, out var timer))
                        return;

                    var timeSinceActivity = (uint)(DateTime.Now - _lastActivity[screenName]).TotalMilliseconds;

                    var isDisplayed = _displayedScreens.ContainsKey(screenName) && _displayedScreens[screenName];
                    var isSecondStage = _secondStageScreens.ContainsKey(screenName) && _secondStageScreens[screenName];

                    if (_cachedDimEnabled)
                    {
                        if (timeSinceActivity < _firstThresholdTime)
                        {
                            uint remaining = _firstThresholdTime - timeSinceActivity;
                            timer.Interval = remaining > 0 ? remaining : 1;
                            timer.Start();
                        }
                        else if (timeSinceActivity >= _firstThresholdTime && timeSinceActivity < (_firstThresholdTime + _secondStageDelay))
                        {
                            if (!isDisplayed)
                            {
                                _displayedScreens[screenName] = true;
                                _secondStageScreens[screenName] = false;
                                DimFirstStage(screenName);
                            }
                            
                            uint remaining = (_firstThresholdTime + _secondStageDelay) - timeSinceActivity;
                            timer.Interval = remaining > 0 ? remaining : 1;
                            timer.Start();
                        }
                        else
                        {
                            if (!isSecondStage)
                            {
                                _displayedScreens[screenName] = true;
                                _secondStageScreens[screenName] = true;
                                DimSecondStage(screenName);
                            }
                        }
                    }
                    else // Dim Disabled, skip to second stage
                    {
                        if (timeSinceActivity < _secondStageDelay)
                        {
                            uint remaining = _secondStageDelay - timeSinceActivity;
                            timer.Interval = remaining > 0 ? remaining : 1;
                            timer.Start();
                        }
                        else
                        {
                            if (!isSecondStage)
                            {
                                _displayedScreens[screenName] = true;
                                _secondStageScreens[screenName] = true;
                                DimSecondStage(screenName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error in ScreenSaverManager Timer Elapsed: {ex.Message}");
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
                
                foreach (var screenName in _displayedScreens.Keys.ToList())
                {
                    if (_displayedScreens[screenName])
                    {
                        _displayedScreens[screenName] = false;
                        _secondStageScreens[screenName] = false;
                        WakeScreen(screenName);
                    }
                }
                
                foreach (var timer in _timers.Values) timer.Stop();
                
                // Clear any existing unpause timer
                if (_unpauseTimer != null)
                {
                    _unpauseTimer.Stop();
                    _unpauseTimer.Dispose();
                    _unpauseTimer = null;
                }

                if (minutes.HasValue && minutes.Value > 0)
                {
                    _unpauseTimer = new System.Timers.Timer(minutes.Value * 60 * 1000);
                    _unpauseTimer.AutoReset = false;
                    _unpauseTimer.Elapsed += (s, e) => Resume();
                    _unpauseTimer.Start();
                }

                _logger.Log("Screensaver paused");
            }
        }

        public void Resume()
        {
            lock (_stateLock)
            {
                _paused = false;
                _pauseEndTime = null;
                
                if (_unpauseTimer != null)
                {
                    _unpauseTimer.Stop();
                    _unpauseTimer.Dispose();
                    _unpauseTimer = null;
                }
                
                foreach (var screenName in _cachedScreenNames)
                {
                    _lastActivity[screenName] = DateTime.Now;
                    if (_timers.TryGetValue(screenName, out var timer))
                    {
                        timer.Interval = GetInitialInterval();
                        timer.Start();
                    }
                }
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
                    foreach (var scr in _cachedScreenNames)
                    {
                        _lastActivity[scr] = DateTime.Now;
                        _displayedScreens[scr] = false;
                        _secondStageScreens[scr] = false;
                        WakeScreen(scr);
                        
                        if (_timers.TryGetValue(scr, out var timer))
                        {
                            timer.Interval = GetInitialInterval();
                            timer.Start();
                        }
                    }
                }
                else
                {
                    foreach (var scr in _cachedScreenNames)
                    {
                        _displayedScreens[scr] = true;
                        _secondStageScreens[scr] = true;
                        _lastActivity[scr] = DateTime.Now.AddDays(-1); 
                        DimSecondStage(scr);
                        
                        if (_timers.TryGetValue(scr, out var timer)) timer.Stop();
                    }
                }
                
                _userActivityMonitor.IgnoreInput(TimeSpan.FromSeconds(1));
            }
        }

        public void Dispose()
        {
            lock (_stateLock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                _timers.Clear();

                if (_unpauseTimer != null)
                {
                    _unpauseTimer.Stop();
                    _unpauseTimer.Dispose();
                    _unpauseTimer = null;
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
