using System;
using System.IO;
using System.Reflection;

namespace OLEDScreenSaver
{
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public FileLogger()
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logPath = Path.Combine(directory, "log.txt");
        }

        public void Log(string message)
        {
#if DEBUG
            WriteToFile("INFO", message);
#endif
        }

        public void Error(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            WriteToFile("ERROR", fullMessage);
        }

        private void WriteToFile(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    using (var writer = new StreamWriter(_logPath, true))
                    {
                        writer.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss} [{level}] {message}");
                    }
                }
            }
            catch
            {
                // Last resort fallback - can't really log a logging failure easily without a secondary logger
            }
        }
    }
}
