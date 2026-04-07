using System;

namespace OLEDScreenSaver
{
    public class UserActivityEventArgs : EventArgs
    {
        public string ScreenName { get; }
        public bool IsActive { get; }

        public UserActivityEventArgs(string screenName, bool isActive)
        {
            ScreenName = screenName;
            IsActive = isActive;
        }
    }

    public interface IUserActivityMonitor : IDisposable
    {
        /// <summary>
        /// Starts monitoring global user activity (mouse/keyboard).
        /// </summary>
        void Start();

        /// <summary>
        /// Stops monitoring user activity.
        /// </summary>
        void Stop();

        /// <summary>
        /// Fired when user activity is detected on a specific screen.
        /// </summary>
        event EventHandler<UserActivityEventArgs> OnUserActivity;

        /// <summary>
        /// Temporarily ignores user input (e.g., just after a hotkey is pressed).
        /// </summary>
        /// <param name="duration">Duration to ignore input for.</param>
        void IgnoreInput(TimeSpan duration);
    }
}
