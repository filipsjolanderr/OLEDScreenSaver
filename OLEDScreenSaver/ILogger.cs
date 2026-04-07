using System;

namespace OLEDScreenSaver
{
    public interface ILogger
    {
        void Log(string message);
        void Error(string message, Exception ex = null);
    }
}
