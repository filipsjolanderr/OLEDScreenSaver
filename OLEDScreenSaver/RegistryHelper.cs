using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public class RegistryHelper
    {
        public static bool SuppressDialogs = false;
        const String DEFAULT_OLED_AWAITED_NAME = "LG TV SSCR";
        const String DEFAULT_TIMEOUT = "5.0";
        const String DEFAULT_SECOND_STAGE_TIMEOUT = "1.0";
        const String DEFAULT_POLL_RATE = "500";
        public static void InitValues()
        {
            if (!RegistryValuesSet())
            {
                SaveTimeout(DEFAULT_TIMEOUT);
                SaveScreenName(DEFAULT_OLED_AWAITED_NAME);
                SavePollRate(DEFAULT_POLL_RATE);
                SaveDimEnabled(true);
                SaveDimPercentage("50");
                SaveAnimationDuration("1000");
            }
        }

        public static Boolean RegistryValuesSet()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            if (key == null) { return false; }
            var value = key.GetValue("Timeout", "No Value");
            if (value.ToString() == "No Value") { return false; }
            // Check both old and new format for screen names
            value = key.GetValue("ScreenNames", "No Value");
            if (value.ToString() == "No Value")
            {
                value = key.GetValue("ScreenName", "No Value");
                if (value.ToString() == "No Value") { return false; }
            }
            value = key.GetValue("PollRate", "No Value");
            if (value.ToString() == "No Value") { return false; }
            value = key.GetValue("DimEnabled", "No Value");
            if (value.ToString() == "No Value") { return false; }
            value = key.GetValue("DimPercentage", "No Value");
            if (value.ToString() == "No Value") { return false; }
            value = key.GetValue("AnimationDuration", "No Value");
            if (value.ToString() == "No Value") { return false; }
            return true;
        }

        public static Boolean SaveTimeout(String timeout)
        {
            if (!double.TryParse(timeout, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutValue) || timeoutValue <= 0)
            {
                var message = "The timeout value must be a positive number (e.g., 0.1 for 6 seconds, 0.5 for 30 seconds, 1 for 1 minute).";
                var caption = "Error While Setting Timeout";
                var buttons = MessageBoxButtons.OK;
                if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
                return false;
            }
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("Timeout", timeout.ToString());
            key.Close();
            return true;
        }

        public static double LoadTimeout()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            double value;
            if (!double.TryParse(DEFAULT_TIMEOUT, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                value = 5.0; // Hardcoded fallback
            }
            if (key != null)
            {
                var raw = key.GetValue("Timeout", null);
                if (raw != null)
                {
                    if (!double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        if (!double.TryParse(DEFAULT_TIMEOUT, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        {
                            value = 5.0; // Hardcoded fallback
                        }
                    }
                }
                key.Close();
            }
            return value;
        }

        public static Boolean SaveSecondStageTimeout(String timeout)
        {
            if (!double.TryParse(timeout, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutValue) || timeoutValue < 0)
            {
                // Only show MessageBox if not in a test environment (no message box in headless tests)
                if (System.Diagnostics.Debugger.IsAttached || Environment.UserInteractive)
                {
                    try
                    {
                        var message = "The second stage timeout value must be zero or a positive number (e.g., 0.5 for 30 seconds, 1 for 1 minute).";
                        var caption = "Error While Setting Second Stage Timeout";
                        var buttons = MessageBoxButtons.OK;
                        if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
                    }
                    catch
                    {
                        // MessageBox might fail in test environments, just log it
                        LogHelper.Log($"Invalid second stage timeout value: {timeout}");
                    }
                }
                else
                {
                    LogHelper.Log($"Invalid second stage timeout value: {timeout}");
                }
                return false;
            }
            try
            {
                var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
                if (key != null)
                {
                    key.SetValue("SecondStageTimeout", timeout.ToString());
                    key.Close();
                    LogHelper.Log($"Saved second stage timeout: {timeout}");
                    return true;
                }
                LogHelper.Log("Failed to create registry key for second stage timeout");
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error saving second stage timeout: {ex.Message}");
                return false;
            }
        }

        public static double LoadSecondStageTimeout()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            double value;
            if (!double.TryParse(DEFAULT_SECOND_STAGE_TIMEOUT, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                value = 1.0; // Hardcoded fallback
            }
            if (key != null)
            {
                var raw = key.GetValue("SecondStageTimeout", null);
                if (raw != null)
                {
                    if (!double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        if (!double.TryParse(DEFAULT_SECOND_STAGE_TIMEOUT, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        {
                            value = 1.0; // Hardcoded fallback
                        }
                    }
                }
                key.Close();
            }
            return value;
        }

        public static Boolean SaveScreenName(String new_name)
        {
            var screen = ScreenHelper.FindScreen(new_name);
            if (screen == null)
            {
                var message = "The screen name could not be found, are you sure you entered it correctly? Check the Windows display settings to get the proper name.";
                var caption = "Error While Getting Screen";
                var buttons = MessageBoxButtons.OK;
                if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
            }
            else
            {
                var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
                key.SetValue("ScreenName", new_name);
                key.Close();
                return true;
            }
            return false;
        }

        public static Boolean SaveScreenNames(List<String> screenNames)
        {
            // Validate all screens exist
            foreach (var name in screenNames)
            {
                var screen = ScreenHelper.FindScreen(name);
                if (screen == null)
                {
                    var message = $"The screen name '{name}' could not be found, are you sure you entered it correctly? Check the Windows display settings to get the proper name.";
                    var caption = "Error While Getting Screen";
                    var buttons = MessageBoxButtons.OK;
                    if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
                    return false;
                }
            }

            // Save as comma-separated string
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("ScreenNames", string.Join(",", screenNames));
            key.Close();
            return true;
        }

        public static String LoadScreenName()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            var value = DEFAULT_OLED_AWAITED_NAME;
            if (key != null)
            {
                value = key.GetValue("ScreenName").ToString();
                key.Close();
            }
            return value;
        }

        public static List<String> LoadScreenNames()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            var screenNames = new List<String>();

            if (key != null)
            {
                // Try to load new format (comma-separated)
                var screenNamesValue = key.GetValue("ScreenNames", "No Value");
                if (screenNamesValue != null && screenNamesValue.ToString() != "No Value")
                {
                    var names = screenNamesValue.ToString().Split(',');
                    foreach (var name in names)
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            screenNames.Add(name.Trim());
                        }
                    }
                }
                else
                {
                    // Fallback to old format (single screen)
                    var screenNameValue = key.GetValue("ScreenName", "No Value");
                    if (screenNameValue != null && screenNameValue.ToString() != "No Value")
                    {
                        screenNames.Add(screenNameValue.ToString());
                    }
                }
                key.Close();
            }

            // If no screens found, use default
            if (screenNames.Count == 0)
            {
                screenNames.Add(DEFAULT_OLED_AWAITED_NAME);
            }

            return screenNames;
        }

        public static Boolean SavePollRate(String pollrate)
        {
            if (!int.TryParse(pollrate, out _))
            {
                var message = "The poll rate value is not an integer.";
                var caption = "Error While Setting poll rate";
                var buttons = MessageBoxButtons.OK;
                if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
            }
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("PollRate", pollrate.ToString());
            key.Close();
            return true;
        }

        public static int LoadPollRate()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            int value;
            if (!int.TryParse(DEFAULT_POLL_RATE, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                value = 500; // Hardcoded fallback
            }
            if (key != null)
            {
                var raw = key.GetValue("PollRate", null);
                if (raw != null)
                {
                    if (!int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    {
                        if (!int.TryParse(DEFAULT_POLL_RATE, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        {
                            value = 500; // Hardcoded fallback
                        }
                    }
                }
                key.Close();
            }
            return value;
        }

        public static Boolean SaveDimEnabled(Boolean enabled)
        {
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("DimEnabled", enabled.ToString());
            key.Close();
            return true;
        }

        public static Boolean LoadDimEnabled()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            var value = true; // default 
            if (key != null)
            {
                var raw = key.GetValue("DimEnabled", null);
                if (raw != null)
                {
                    if (!Boolean.TryParse(raw.ToString(), out value))
                    {
                        value = true;
                    }
                }
                key.Close();
            }
            return value;
        }

        public static Boolean SaveDimPercentage(String percentage)
        {
            if (!int.TryParse(percentage, out var percentageValue) || percentageValue < 10 || percentageValue > 100)
            {
                var message = "The dim percentage value must be an integer between 10 and 100.";
                var caption = "Error While Setting dim percentage";
                var buttons = MessageBoxButtons.OK;
                if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
                return false;
            }
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("DimPercentage", percentage.ToString());
            key.Close();
            return true;
        }

        public static int LoadDimPercentage()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            var value = 50; // default 
            if (key != null)
            {
                var raw = key.GetValue("DimPercentage", null);
                if (raw != null)
                {
                    if (!int.TryParse(raw.ToString(), out value))
                    {
                        value = 50;
                    }
                }
                key.Close();
            }
            if (value < 10) value = 10;
            if (value > 100) value = 100;
            return value;
        }

        public static Boolean SaveAnimationDuration(String durationMs)
        {
            if (!int.TryParse(durationMs, out var durationValue) || durationValue < 0 || durationValue > 5000)
            {
                var message = "The animation duration must be an integer between 0 and 5000 (milliseconds).";
                var caption = "Error While Setting animation duration";
                var buttons = MessageBoxButtons.OK;
                if (!SuppressDialogs) MessageBox.Show(message, caption, buttons);
                return false;
            }
            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OLEDScreenSaver");
            key.SetValue("AnimationDuration", durationMs.ToString());
            key.Close();
            return true;
        }

        public static int LoadAnimationDuration()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver");
            var value = 1000; // default 
            if (key != null)
            {
                var raw = key.GetValue("AnimationDuration", null);
                if (raw != null)
                {
                    if (!int.TryParse(raw.ToString(), out value))
                    {
                        value = 1000;
                    }
                }
                key.Close();
            }
            if (value < 0) value = 0;
            if (value > 5000) value = 5000;
            return value;
        }

        public static void SetStartup(Boolean enabled)
        {
            var key = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enabled)
            {
                key.SetValue("OLEDScreenSaver", Application.ExecutablePath);
            }
            else
            {
                key.DeleteValue("OLEDScreenSaver", false);
            }
        }

        public static Boolean LoadStartup()
        {
            var key = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var value = key.GetValue("OLEDScreenSaver", "No Value").ToString();
            if (value == "No Value")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
