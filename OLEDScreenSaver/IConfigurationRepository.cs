using System;
using System.Collections.Generic;

namespace OLEDScreenSaver
{
    public interface IConfigurationRepository
    {
        double LoadTimeout();
        bool SaveTimeout(string timeout);

        double LoadSecondStageTimeout();
        bool SaveSecondStageTimeout(string timeout);

        string LoadScreenName();
        bool SaveScreenName(string newName);

        List<string> LoadScreenNames();
        bool SaveScreenNames(List<string> screenNames);

        int LoadPollRate();
        bool SavePollRate(string pollrate);

        bool LoadDimEnabled();
        bool SaveDimEnabled(bool enabled);

        int LoadDimPercentage();
        bool SaveDimPercentage(string percentage);

        int LoadAnimationDuration();
        bool SaveAnimationDuration(string durationMs);

        bool LoadStartup();
        void SetStartup(bool enabled);
        
        bool IsConfigured();
        void InitializeDefaultValues();
    }
}
