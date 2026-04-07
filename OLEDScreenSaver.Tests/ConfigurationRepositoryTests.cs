using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLEDScreenSaver;
using Moq; // Wait, I don't know if Moq is available. I'll check packages.config.

namespace OLEDScreenSaver.Tests
{
    [TestClass]
    public class ConfigurationRepositoryTests
    {
        private RegistryConfigurationRepository _repo;
        private ILogger _logger;

        [TestInitialize]
        public void TestInitialize()
        {
            _logger = new NullLogger(); // Simple manual mock
            _repo = new RegistryConfigurationRepository(_logger);
            
            // Note: In a real world, we'd use a different registry path for tests.
            // For now, we'll follow the existing test pattern but ensure it uses the new class.
        }

        [TestMethod]
        public void TestSaveAndLoadTimeout()
        {
            var testTimeout = "2.5";
            var saved = _repo.SaveTimeout(testTimeout);
            Assert.IsTrue(saved);

            var loaded = _repo.LoadTimeout();
            Assert.AreEqual(2.5, loaded, 0.001);
        }

        [TestMethod]
        public void TestSaveAndLoadSecondStageTimeout()
        {
            var testTimeout = "1.5";
            var saved = _repo.SaveSecondStageTimeout(testTimeout);
            Assert.IsTrue(saved);

            var loaded = _repo.LoadSecondStageTimeout();
            Assert.AreEqual(1.5, loaded, 0.001);
        }

        private class NullLogger : ILogger
        {
            public void Log(string message) { }
            public void Error(string message, Exception ex) { }
        }
    }
}
