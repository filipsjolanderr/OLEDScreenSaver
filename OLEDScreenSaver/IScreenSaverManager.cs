using System;

namespace OLEDScreenSaver
{
    public class ScreenEventArgs : EventArgs
    {
        public string ScreenName { get; }

        public ScreenEventArgs(string screenName)
        {
            ScreenName = screenName;
        }
    }

    public interface IScreenSaverManager : IDisposable
    {
        /// <summary>
        /// Event fired when a screen should enter the first dimming stage.
        /// </summary>
        event EventHandler<ScreenEventArgs> OnFirstStageDim;

        /// <summary>
        /// Event fired when a screen should enter the second, full dimming stage.
        /// </summary>
        event EventHandler<ScreenEventArgs> OnSecondStageDim;

        /// <summary>
        /// Event fired when a screen should be awakened (dimming removed).
        /// </summary>
        event EventHandler<ScreenEventArgs> OnWake;

        /// <summary>
        /// Launches the internal state machine / timer.
        /// </summary>
        void Launch();

        /// <summary>
        /// Pauses the screensaver logic.
        /// </summary>
        /// <param name="minutes">Optional duration to pause. If null, pauses indefinitely.</param>
        void Pause(int? minutes = null);

        /// <summary>
        /// Resumes the screensaver if it was paused.
        /// </summary>
        void Resume();

        /// <summary>
        /// Toggles the screensaver state for all tracked screens (forces dim or wakes them up).
        /// </summary>
        void ToggleScreensaver();

        /// <summary>
        /// Signals that the configuration might have changed, prompting a reload of settings if needed.
        /// </summary>
        void ReloadConfiguration();
    }
}
