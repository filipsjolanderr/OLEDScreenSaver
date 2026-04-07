using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;

namespace OLEDScreenSaver
{
    public class RegistryConfigurationRepository : IConfigurationRepository
    {
        private const string REGISTRY_PATH = @"SOFTWARE\OLEDScreenSaver";
        private readonly ILogger _logger;

        private const string DEFAULT_OLED_AWAITED_NAME = "LG TV SSCR";
        private const string DEFAULT_TIMEOUT = "5.0";
        private const string DEFAULT_SECOND_STAGE_TIMEOUT = "1.0";
        private const string DEFAULT_POLL_RATE = "500";

        public RegistryConfigurationRepository(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsConfigured()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
            {
                if (key == null) return false;
                
                return key.GetValue("Timeout") != null &&
                       (key.GetValue("ScreenNames") != null || key.GetValue("ScreenName") != null) &&
                       key.GetValue("PollRate") != null &&
                       key.GetValue("DimEnabled") != null &&
                       key.GetValue("DimPercentage") != null &&
                       key.GetValue("AnimationDuration") != null;
            }
        }

        public void InitializeDefaultValues()
        {
            if (!IsConfigured())
            {
                SaveTimeout(DEFAULT_TIMEOUT);
                SaveScreenName(DEFAULT_OLED_AWAITED_NAME);
                SavePollRate(DEFAULT_POLL_RATE);
                SaveDimEnabled(true);
                SaveDimPercentage("50");
                SaveAnimationDuration("500");
                SaveSecondStageTimeout(DEFAULT_SECOND_STAGE_TIMEOUT);
            }
        }

        public double LoadTimeout()
        {
            var raw = LoadValue("Timeout", DEFAULT_TIMEOUT);
            if (double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return 5.0;
        }

        public bool SaveTimeout(string timeout)
        {
            if (!double.TryParse(timeout, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) || val <= 0)
            {
                _logger.Log($"Invalid timeout value: {timeout}");
                return false;
            }
            return SaveValue("Timeout", timeout);
        }

        public double LoadSecondStageTimeout()
        {
            var raw = LoadValue("SecondStageTimeout", DEFAULT_SECOND_STAGE_TIMEOUT);
            if (double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return 1.0;
        }

        public bool SaveSecondStageTimeout(string timeout)
        {
            if (!double.TryParse(timeout, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) || val < 0)
            {
                _logger.Log($"Invalid second stage timeout value: {timeout}");
                return false;
            }
            return SaveValue("SecondStageTimeout", timeout);
        }

        public string LoadScreenName()
        {
            return LoadValue("ScreenName", DEFAULT_OLED_AWAITED_NAME).ToString();
        }

        public bool SaveScreenName(string newName)
        {
            return SaveValue("ScreenName", newName);
        }

        public List<string> LoadScreenNames()
        {
            var screenNames = new List<string>();
            var screenNamesValue = LoadValue("ScreenNames", null) as string;
            
            if (!string.IsNullOrWhiteSpace(screenNamesValue))
            {
                foreach (var name in screenNamesValue.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(name)) screenNames.Add(name.Trim());
                }
            }
            else
            {
                var singleName = LoadScreenName();
                if (!string.IsNullOrWhiteSpace(singleName)) screenNames.Add(singleName);
            }

            if (screenNames.Count == 0) screenNames.Add(DEFAULT_OLED_AWAITED_NAME);
            return screenNames;
        }

        public bool SaveScreenNames(List<string> screenNames)
        {
            return SaveValue("ScreenNames", string.Join(",", screenNames));
        }

        public int LoadPollRate()
        {
            var raw = LoadValue("PollRate", DEFAULT_POLL_RATE);
            if (int.TryParse(raw.ToString(), out var value)) return value;
            return 500;
        }

        public bool SavePollRate(string pollrate)
        {
            if (!int.TryParse(pollrate, out _)) return false;
            return SaveValue("PollRate", pollrate);
        }

        public bool LoadDimEnabled()
        {
            var raw = LoadValue("DimEnabled", "True");
            if (bool.TryParse(raw.ToString(), out var value)) return value;
            return true;
        }

        public bool SaveDimEnabled(bool enabled)
        {
            return SaveValue("DimEnabled", enabled.ToString());
        }

        public int LoadDimPercentage()
        {
            var raw = LoadValue("DimPercentage", "50");
            if (int.TryParse(raw.ToString(), out var value))
            {
                return Math.Max(10, Math.Min(100, value));
            }
            return 50;
        }

        public bool SaveDimPercentage(string percentage)
        {
            if (!int.TryParse(percentage, out var val) || val < 10 || val > 100) return false;
            return SaveValue("DimPercentage", percentage);
        }

        public int LoadAnimationDuration()
        {
            var raw = LoadValue("AnimationDuration", "1000");
            if (int.TryParse(raw.ToString(), out var value))
            {
                return Math.Max(0, Math.Min(5000, value));
            }
            return 1000;
        }

        public bool SaveAnimationDuration(string durationMs)
        {
            if (!int.TryParse(durationMs, out var val) || val < 0 || val > 5000) return false;
            return SaveValue("AnimationDuration", durationMs);
        }

        public bool LoadStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue("OLEDScreenSaver") != null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load startup setting", ex);
                return false;
            }
        }

        public void SetStartup(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enabled) key.SetValue("OLEDScreenSaver", System.Windows.Forms.Application.ExecutablePath);
                        else key.DeleteValue("OLEDScreenSaver", false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to set startup setting", ex);
            }
        }

        private bool SaveValue(string valueName, object value)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
                {
                    key?.SetValue(valueName, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save {valueName} to registry", ex);
                return false;
            }
        }

        private object LoadValue(string valueName, object defaultValue)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
                {
                    return key?.GetValue(valueName) ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load {valueName} from registry", ex);
                return defaultValue;
            }
        }
    }
}
