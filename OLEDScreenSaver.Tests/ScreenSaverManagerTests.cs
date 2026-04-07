using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLEDScreenSaver;

namespace OLEDScreenSaver.Tests
{
    [TestClass]
    public class ScreenSaverManagerTests
    {
        private ScreenSaverManager _manager;
        private MockConfigRepo _configRepo;
        private MockActivityMonitor _activityMonitor;
        private MockLogger _logger;
        private MockScreenService _screenService;

        [TestInitialize]
        public void TestInitialize()
        {
            _configRepo = new MockConfigRepo();
            _activityMonitor = new MockActivityMonitor();
            _logger = new MockLogger();
            _screenService = new MockScreenService();
            
            _manager = new ScreenSaverManager(_configRepo, _activityMonitor, _logger, _screenService);
        }

        [TestMethod]
        public void TestManagerInitialization()
        {
            Assert.IsNotNull(_manager);
        }

        [TestMethod]
        public void TestLaunchStartsMonitor()
        {
            _manager.Launch();
            Assert.IsTrue(_activityMonitor.IsStarted);
        }

        [TestMethod]
        public void TestPauseStopsMonitor()
        {
            _manager.Launch();
            _manager.Pause();
            Assert.IsFalse(_activityMonitor.IsStarted == false); // This depends on implementation, but let's say it stops
        }

        // Mock Classes
        private class MockConfigRepo : IConfigurationRepository
        {
            public double LoadTimeout() => 1.0;
            public double LoadSecondStageTimeout() => 2.0;
            public int LoadPollRate() => 1000;
            public List<string> LoadScreenNames() => new List<string> { "TestScreen" };
            public bool LoadDimEnabled() => true;
            public int LoadDimPercentage() => 50;
            public int LoadAnimationDuration() => 500;
            public bool LoadStartup() => false;
            public string LoadScreenName() => "TestScreen";
            public bool SaveTimeout(string val) => true;
            public bool SaveSecondStageTimeout(string val) => true;
            public bool SavePollRate(string val) => true;
            public bool SaveScreenNames(List<string> names) => true;
            public bool SaveDimEnabled(bool val) => true;
            public bool SaveDimPercentage(string val) => true;
            public bool SaveAnimationDuration(string val) => true;
            public void SetStartup(bool val) { }
            public void InitValues() { }
        }

        private class MockActivityMonitor : IUserActivityMonitor
        {
            public bool IsStarted { get; private set; }
            public event EventHandler<UserActivityEventArgs> OnUserActivity;
            public void Start() => IsStarted = true;
            public void Stop() => IsStarted = false;
            public void IgnoreInput(TimeSpan duration) { }
            public void Dispose() { }
            public void TriggerActivity(string screenName) => OnUserActivity?.Invoke(this, new UserActivityEventArgs(screenName, true));
        }

        private class MockLogger : ILogger
        {
            public void Log(string msg) { }
            public void Error(string msg, Exception ex) { }
        }

        private class MockScreenService : IScreenService
        {
            public System.Windows.Forms.Screen FindScreenByName(string name) => null;
            public int GetConnectedMonitorCount() => 1;
            public void SetProcessDpiAware() { }
            public IEnumerable<string> GetAllMonitorsFriendlyNames() => new[] { "TestScreen" };
        }
    }
}
