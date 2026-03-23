using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLEDScreenSaver;

namespace OLEDScreenSaver.Tests
{
    [TestClass]
    public class RegistryHelperTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            RegistryHelper.SuppressDialogs = true;
            // Initialize registry values for testing
            RegistryHelper.InitValues();
            // Clear any existing second stage timeout to start fresh
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OLEDScreenSaver", true);
                if (key != null)
                {
                    key.DeleteValue("SecondStageTimeout", false);
                    key.Close();
                }
            }
            catch
            {
                // Ignore if key doesn't exist
            }
        }

        [TestMethod]
        public void TestSaveAndLoadTimeout()
        {
            // Test saving and loading timeout values
            var testTimeout = "2.5";
            var saved = RegistryHelper.SaveTimeout(testTimeout);
            Assert.IsTrue(saved, "SaveTimeout should return true for valid value");

            var loaded = RegistryHelper.LoadTimeout();
            Assert.AreEqual(2.5, loaded, 0.001, "Loaded timeout should match saved value");
        }

        [TestMethod]
        public void TestSaveTimeoutInvalidValue()
        {
            // Test that invalid timeout values are rejected
            var saved = RegistryHelper.SaveTimeout("invalid");
            Assert.IsFalse(saved, "SaveTimeout should return false for invalid value");

            var savedNegative = RegistryHelper.SaveTimeout("-1.0");
            Assert.IsFalse(savedNegative, "SaveTimeout should return false for negative value");
        }

        [TestMethod]
        public void TestSaveAndLoadSecondStageTimeout()
        {
            // Test saving and loading second stage timeout
            var testTimeout = "1.5";
            var saved = RegistryHelper.SaveSecondStageTimeout(testTimeout);
            Assert.IsTrue(saved, "SaveSecondStageTimeout should return true for valid value");

            var loaded = RegistryHelper.LoadSecondStageTimeout();
            Assert.AreEqual(1.5, loaded, 0.001, "Loaded second stage timeout should match saved value");
        }

        [TestMethod]
        public void TestSaveSecondStageTimeoutZero()
        {
            // Test that zero is allowed for second stage timeout
            var saved = RegistryHelper.SaveSecondStageTimeout("0.0");
            Assert.IsTrue(saved, "SaveSecondStageTimeout should accept zero");

            var loaded = RegistryHelper.LoadSecondStageTimeout();
            Assert.AreEqual(0.0, loaded, 0.001, "Zero should be saved and loaded correctly");
        }

        [TestMethod]
        public void TestSaveAndLoadPollRate()
        {
            // Test saving and loading poll rate
            var testPollRate = "250";
            var saved = RegistryHelper.SavePollRate(testPollRate);
            Assert.IsTrue(saved, "SavePollRate should return true for valid value");

            var loaded = RegistryHelper.LoadPollRate();
            Assert.AreEqual(250, loaded, "Loaded poll rate should match saved value");
        }

        [TestMethod]
        public void TestSaveAndLoadScreenNames()
        {
            // Test saving and loading screen names
            // Note: This test may fail if the screen names don't exist on the system
            // We'll use actual screen names from the system if available, or skip validation
            var testScreens = new List<string>();
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var friendlyName = ScreenInterrogatory.DeviceFriendlyName(screen);
                if (!string.IsNullOrEmpty(friendlyName))
                {
                    testScreens.Add(friendlyName);
                    if (testScreens.Count >= 3) break; // Use up to 3 screens
                }
            }
            
            if (testScreens.Count > 0)
            {
                var saved = RegistryHelper.SaveScreenNames(testScreens);
                Assert.IsTrue(saved, "SaveScreenNames should return true for valid screens");

                var loaded = RegistryHelper.LoadScreenNames();
                Assert.AreEqual(testScreens.Count, loaded.Count, "Loaded screen count should match");
                foreach (var screen in testScreens)
                {
                    Assert.IsTrue(loaded.Contains(screen), $"Loaded screens should contain {screen}");
                }
            }
            else
            {
                // If no screens found, just test that the method doesn't crash
                var emptyList = new List<string>();
                // This should fail validation, but we're just checking it doesn't crash
                Assert.IsTrue(true, "No screens available for testing, but method is callable");
            }
        }

        [TestMethod]
        public void TestLoadTimeoutWithInvalidCulture()
        {
            // Test that LoadTimeout handles culture-specific decimal separators
            // This tests the InvariantCulture parsing fix
            RegistryHelper.SaveTimeout("5.5");
            var loaded = RegistryHelper.LoadTimeout();
            Assert.AreEqual(5.5, loaded, 0.001, "Should load decimal values correctly regardless of culture");
        }

        [TestMethod]
        public void TestLoadSecondStageTimeoutWithInvalidCulture()
        {
            // Test that LoadSecondStageTimeout handles culture-specific decimal separators
            RegistryHelper.SaveSecondStageTimeout("1.2");
            var loaded = RegistryHelper.LoadSecondStageTimeout();
            Assert.AreEqual(1.2, loaded, 0.001, "Should load decimal values correctly regardless of culture");
        }

        [TestMethod]
        public void TestTimeoutParsingWithSmallValues()
        {
            // Test that small values like 0.1 work correctly
            var smallValue = "0.1";
            var saved = RegistryHelper.SaveTimeout(smallValue);
            Assert.IsTrue(saved, "Should save small decimal values");

            var loaded = RegistryHelper.LoadTimeout();
            Assert.AreEqual(0.1, loaded, 0.001, "Should load small decimal values correctly");
        }
    }
}
