using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Karmetric.Background.Infrastructure
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logPath;

        public FileLoggerProvider()
        {
            try
            {
                string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logPath = Path.Combine(localData, "Karmetric", "logs");
                Directory.CreateDirectory(_logPath);
            }
            catch { /* Best effort */ }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logPath);
        }

        public void Dispose() { }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logPath;
        private static object _lock = new object();

        public FileLogger(string categoryName, string logPath)
        {
            _categoryName = categoryName;
            _logPath = logPath;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Simple filter: mainly for Information and above, or Debug if requested
            return logLevel >= LogLevel.Debug; 
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (_logPath == null) return;

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}";
            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }

            var fileName = $"Karmetric-{DateTime.Now:yyyyMMdd}.log";
            var fullPath = Path.Combine(_logPath, fileName);

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(fullPath, logEntry + Environment.NewLine);
                }
            }
            catch { /* Ignore logging errors */ }
        }
    }
}
