using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;

namespace OLEDScreenSaver
{
    public class ScreenSaver
    {
        private System.Timers.Timer screenSaverTimer;
        private Action<string> showFormCallback;
        private Action<string> hideFormCallback;
        private Action<string> secondStageCallback;
        private bool paused = false;
        private DateTime? pauseEndTime = null;
        private uint firstThresholdTime = 0;
        private uint secondStageDelay = 0;
        private uint pollrate = 0;
        private object stateLock = new object();
        // Per-screen tracking: screen name -> last mouse activity time
        private Dictionary<string, DateTime> lastOledMouseActivity = new Dictionary<string, DateTime>();
        // Per-screen display state: screen name -> is displayed
        private Dictionary<string, bool> displayedScreens = new Dictionary<string, bool>();
        // Per-screen second stage state: screen name -> is in second stage
        private Dictionary<string, bool> secondStageScreens = new Dictionary<string, bool>();
        private List<string> cachedScreenNames = new List<string>();
        private bool cachedDimEnabled = true;

        public ScreenSaver()
        {
            firstThresholdTime = (uint)(RegistryHelper.LoadTimeout() * 60.0 * 1000.0);
            secondStageDelay = (uint)(RegistryHelper.LoadSecondStageTimeout() * 60.0 * 1000.0);
            pollrate = (uint)RegistryHelper.LoadPollRate();
            InitializeScreenStates();
        }

        private void InitializeScreenStates()
        {
            lock (stateLock)
            {
                cachedScreenNames = RegistryHelper.LoadScreenNames();
                cachedDimEnabled = RegistryHelper.LoadDimEnabled();
                foreach (var screenName in cachedScreenNames)
                {
                    if (!lastOledMouseActivity.ContainsKey(screenName))
                    {
                        lastOledMouseActivity[screenName] = DateTime.Now;
                        displayedScreens[screenName] = false;
                        secondStageScreens[screenName] = false;
                    }
                }
            }
        }

        public void Launch()
        {
            lock (stateLock)
            {
                if (screenSaverTimer != null)
                {
                    screenSaverTimer.Stop();
                    screenSaverTimer.Dispose();
                }
                screenSaverTimer = new System.Timers.Timer(pollrate);
                screenSaverTimer.Elapsed += new System.Timers.ElapsedEventHandler(Tick);
                screenSaverTimer.AutoReset = true;
                screenSaverTimer.Enabled = true;
                LogHelper.Log($"Timer launched with poll rate: {pollrate}ms, first threshold: {firstThresholdTime}ms, second stage delay: {secondStageDelay}ms");
            }
        }

        public void Tick(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                lock (stateLock)
                {
                    if (paused)
                    {
                        return;
                    }

                    // Use cached screen states
                    var screenNames = cachedScreenNames;
                    foreach (var screenName in screenNames)
                    {
                        if (!lastOledMouseActivity.ContainsKey(screenName))
                        {
                            lastOledMouseActivity[screenName] = DateTime.Now;
                            displayedScreens[screenName] = false;
                            secondStageScreens[screenName] = false;
                        }
                    }

                    // Remove screens that are no longer configured
                    var screensToRemove = new List<string>();
                    foreach (var screenName in lastOledMouseActivity.Keys)
                    {
                        if (!screenNames.Contains(screenName))
                        {
                            screensToRemove.Add(screenName);
                        }
                    }
                    foreach (var screenName in screensToRemove)
                    {
                        lastOledMouseActivity.Remove(screenName);
                        displayedScreens.Remove(screenName);
                        secondStageScreens.Remove(screenName);
                    }

                    // Auto-Resume logic
                    if (pauseEndTime.HasValue && DateTime.Now >= pauseEndTime.Value)
                    {
                        LogHelper.Log("Auto-resuming screensaver after pause duration expired.");
                        paused = false;
                        pauseEndTime = null;
                        foreach (var name in screenNames)
                        {
                            lastOledMouseActivity[name] = DateTime.Now;
                        }
                    }

                    if (paused)
                        return;

                    // Check each screen independently
                    foreach (var screenName in screenNames)
                    {
                        if (!lastOledMouseActivity.ContainsKey(screenName))
                            continue;

                        var time = (uint)(DateTime.Now - lastOledMouseActivity[screenName]).TotalMilliseconds;
                        var dimEnabled = cachedDimEnabled;

                        var shouldDisplayFirstStage = dimEnabled && time > firstThresholdTime && time <= (firstThresholdTime + secondStageDelay);
                        var shouldDisplaySecondStage = dimEnabled ? time > (firstThresholdTime + secondStageDelay) : time > secondStageDelay;
                        var isDisplayed = displayedScreens.ContainsKey(screenName) && displayedScreens[screenName];
                        var isSecondStage = secondStageScreens.ContainsKey(screenName) && secondStageScreens[screenName];

                        if (shouldDisplayFirstStage && !isDisplayed && !isSecondStage)
                        {
                            displayedScreens[screenName] = true;
                            secondStageScreens[screenName] = false;
                            OnCreateScreensaver(screenName);
                        }
                        else if (!shouldDisplayFirstStage && !shouldDisplaySecondStage && isDisplayed)
                        {
                            displayedScreens[screenName] = false;
                            secondStageScreens[screenName] = false;
                            OnCloseScreensaver(screenName);
                        }
                        else if (shouldDisplaySecondStage && !isSecondStage)
                        {
                            displayedScreens[screenName] = true;
                            secondStageScreens[screenName] = true;
                            OnEnterSecondStage(screenName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in Tick: {ex.Message}");
            }
        }

        static uint GetLastInputTime()
        {
            try
            {
                uint idleTime = 0;
                var lastInputInfo = new Win32Helper.LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
                lastInputInfo.dwTime = 0;

                var envTicks = (uint)Environment.TickCount;

                if (Win32Helper.GetLastInputInfo(ref lastInputInfo))
                {
                    var lastInputTick = lastInputInfo.dwTime;
                    idleTime = envTicks - lastInputTick;
                }

                return idleTime;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error getting last input time: {ex.Message}");
                return 0;
            }
        }

        private void OnCreateScreensaver(string screenName)
        {
            try
            {
                LogHelper.Log($"Creating screensaver (first dim stage) for screen: {screenName}");
                if (showFormCallback != null)
                {
                    showFormCallback(screenName);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in OnCreateScreensaver for {screenName}: {ex.Message}");
            }
        }

        private void OnCloseScreensaver(string screenName)
        {
            try
            {
                LogHelper.Log($"Closing screensaver for screen: {screenName}");
                if (hideFormCallback != null)
                {
                    hideFormCallback(screenName);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in OnCloseScreensaver for {screenName}: {ex.Message}");
            }
        }

        private void OnEnterSecondStage(string screenName)
        {
            try
            {
                LogHelper.Log($"Entering second dim stage for screen: {screenName}");
                if (secondStageCallback != null)
                {
                    secondStageCallback(screenName);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in OnEnterSecondStage for {screenName}: {ex.Message}");
            }
        }

        public void PauseScreensaver(int? minutes = null)
        {
            lock (stateLock)
            {
                paused = true;
                if (minutes.HasValue)
                {
                    pauseEndTime = DateTime.Now.AddMinutes(minutes.Value);
                }
                else
                {
                    pauseEndTime = null;
                }
                // Hide all displayed screens
                var screensToHide = new List<string>(displayedScreens.Keys);
                foreach (var screenName in screensToHide)
                {
                    if (displayedScreens[screenName])
                    {
                        displayedScreens[screenName] = false;
                        secondStageScreens[screenName] = false;
                        OnCloseScreensaver(screenName);
                    }
                }
                StopTimer();
                LogHelper.Log("Screensaver paused");
            }
        }

        public void ResumeScreensaver()
        {
            lock (stateLock)
            {
                paused = false;
                pauseEndTime = null;
                StartTimer();
                LogHelper.Log("Screensaver resumed");
            }
        }

        public void RegisterShowFormCallback(Action<string> pShowFormCallback)
        {
            showFormCallback = pShowFormCallback;
        }

        public void RegisterHideFormCallback(Action<string> pHideFormCallback)
        {
            hideFormCallback = pHideFormCallback;
        }

        public void RegisterSecondStageCallback(Action<string> pSecondStageCallback)
        {
            secondStageCallback = pSecondStageCallback;
        }

        /// <summary>
        /// Notify the screensaver that the mouse is currently active on a specific OLED screen.
        /// This is used instead of global last input time to decide when to dim OLED screens.
        /// Each screen tracks its own mouse activity independently.
        /// </summary>
        public void NotifyOledMouseActivity(string screenName)
        {
            lock (stateLock)
            {
                if (!lastOledMouseActivity.ContainsKey(screenName))
                {
                    lastOledMouseActivity[screenName] = DateTime.Now;
                    displayedScreens[screenName] = false;
                    secondStageScreens[screenName] = false;
                }
                else
                {
                    lastOledMouseActivity[screenName] = DateTime.Now;
                    // Reset display state if mouse becomes active again
                    if (displayedScreens.ContainsKey(screenName) && displayedScreens[screenName])
                    {
                        displayedScreens[screenName] = false;
                        secondStageScreens[screenName] = false;
                        OnCloseScreensaver(screenName);
                    }
                }
            }
        }

        public void ForceShowScreensaver(string screenName)
        {
            lock (stateLock)
            {
                if (lastOledMouseActivity.ContainsKey(screenName))
                {
                    displayedScreens[screenName] = true;
                    secondStageScreens[screenName] = true;
                    // Force an old timestamp so it doesn't immediately close on next tick
                    lastOledMouseActivity[screenName] = DateTime.Now.AddDays(-1); 
                    OnEnterSecondStage(screenName);
                }
            }
        }

        public void UpdateTimeout()
        {
            lock (stateLock)
            {
                firstThresholdTime = (uint)(RegistryHelper.LoadTimeout() * 60.0 * 1000.0);
                LogHelper.Log($"Timeout updated to: {firstThresholdTime}ms");
            }
        }

        public void UpdateSecondStageTimeout()
        {
            lock (stateLock)
            {
                secondStageDelay = (uint)(RegistryHelper.LoadSecondStageTimeout() * 60.0 * 1000.0);
                LogHelper.Log($"Second stage timeout updated to: {secondStageDelay}ms");
            }
        }

        public void UpdatePollRate()
        {
            lock (stateLock)
            {
                pollrate = (uint)RegistryHelper.LoadPollRate();
                Launch();
            }
        }

        public void UpdateScreenNames()
        {
            lock (stateLock)
            {
                cachedScreenNames = RegistryHelper.LoadScreenNames();
            }
        }

        public void UpdateDimEnabled()
        {
            lock (stateLock)
            {
                cachedDimEnabled = RegistryHelper.LoadDimEnabled();
            }
        }

        public void StartTimer()
        {
            lock (stateLock)
            {
                if (screenSaverTimer != null)
                {
                    screenSaverTimer.Start();
                }
            }
        }

        public void StopTimer()
        {
            lock (stateLock)
            {
                if (screenSaverTimer != null)
                {
                    screenSaverTimer.Stop();
                }
            }
        }

        public void Dispose()
        {
            lock (stateLock)
            {
                if (screenSaverTimer != null)
                {
                    screenSaverTimer.Stop();
                    screenSaverTimer.Dispose();
                    screenSaverTimer = null;
                }
            }
        }
    }
}
