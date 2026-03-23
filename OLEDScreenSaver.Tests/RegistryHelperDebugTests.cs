using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLEDScreenSaver;

namespace OLEDScreenSaver.Tests
{
    [TestClass]
    public class RegistryHelperDebugTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            RegistryHelper.SuppressDialogs = true;
        }

        [TestMethod]
        public void TestSecondStageTimeoutParsing()
        {
            // Test that parsing works correctly
            var testValue = "1.5";
            var canParse = double.TryParse(testValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue);
            Assert.IsTrue(canParse, "Should be able to parse 1.5");
            Assert.AreEqual(1.5, parsedValue, 0.001, "Parsed value should be 1.5");

            var zeroValue = "0.0";
            var canParseZero = double.TryParse(zeroValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedZero);
            Assert.IsTrue(canParseZero, "Should be able to parse 0.0");
            Assert.AreEqual(0.0, parsedZero, 0.001, "Parsed value should be 0.0");
        }

        [TestMethod]
        public void TestSaveSecondStageTimeoutDirect()
        {
            // Test SaveSecondStageTimeout directly with logging
            try
            {
                var result = RegistryHelper.SaveSecondStageTimeout("1.5");
                Assert.IsTrue(result, "SaveSecondStageTimeout should return true for 1.5");
            }
            catch (Exception ex)
            {
                Assert.Fail($"SaveSecondStageTimeout threw exception: {ex.Message}");
            }
        }
    }
}
