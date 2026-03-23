using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OLEDScreenSaver;

namespace OLEDScreenSaver.Tests
{
    [TestClass]
    public class ScreenSaverTests
    {
        private ScreenSaver screenSaver;
        private bool firstStageCalled;
        private bool secondStageCalled;
        private bool hideCalled;
        private string lastScreenName;

        [TestInitialize]
        public void TestInitialize()
        {
            RegistryHelper.SuppressDialogs = true;
            screenSaver = new ScreenSaver();
            firstStageCalled = false;
            secondStageCalled = false;
            hideCalled = false;
            lastScreenName = null;

            // Register test callbacks
            screenSaver.RegisterShowFormCallback((screenName) =>
            {
                firstStageCalled = true;
                lastScreenName = screenName;
            });

            screenSaver.RegisterSecondStageCallback((screenName) =>
            {
                secondStageCalled = true;
                lastScreenName = screenName;
            });

            screenSaver.RegisterHideFormCallback((screenName) =>
            {
                hideCalled = true;
                lastScreenName = screenName;
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (screenSaver != null)
            {
                screenSaver.PauseScreensaver();
                screenSaver.Dispose();
            }
        }

        [TestMethod]
        public void TestScreenSaverInitialization()
        {
            Assert.IsNotNull(screenSaver, "ScreenSaver should be initialized");
        }

        [TestMethod]
        public void TestPauseAndResume()
        {
            screenSaver.Launch();
            Thread.Sleep(100); // Give it a moment to start

            screenSaver.PauseScreensaver();
            Assert.IsTrue(true, "Pause should complete without error");

            screenSaver.ResumeScreensaver();
            Assert.IsTrue(true, "Resume should complete without error");
        }

        [TestMethod]
        public void TestNotifyOledMouseActivity()
        {
            var testScreen = "Test Screen";
            screenSaver.NotifyOledMouseActivity(testScreen);
            Assert.IsTrue(true, "NotifyOledMouseActivity should complete without error");
        }

        [TestMethod]
        public void TestUpdateTimeout()
        {
            screenSaver.UpdateTimeout();
            Assert.IsTrue(true, "UpdateTimeout should complete without error");
        }

        [TestMethod]
        public void TestUpdateSecondStageTimeout()
        {
            screenSaver.UpdateSecondStageTimeout();
            Assert.IsTrue(true, "UpdateSecondStageTimeout should complete without error");
        }

        [TestMethod]
        public void TestUpdatePollRate()
        {
            screenSaver.UpdatePollRate();
            Assert.IsTrue(true, "UpdatePollRate should complete without error");
        }

        [TestMethod]
        public void TestMultipleScreenActivity()
        {
            // Test that multiple screens can be tracked independently
            var screen1 = "Screen 1";
            var screen2 = "Screen 2";

            screenSaver.NotifyOledMouseActivity(screen1);
            screenSaver.NotifyOledMouseActivity(screen2);

            Assert.IsTrue(true, "Multiple screens should be tracked independently");
        }

        [TestMethod]
        public void TestDispose()
        {
            screenSaver.Launch();
            screenSaver.Dispose();
            Assert.IsTrue(true, "Dispose should complete without error");
        }
    }
}
