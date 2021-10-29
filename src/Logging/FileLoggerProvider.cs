using Microsoft.Extensions.Logging;
using System;

namespace Majenka.Logging
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private ILogger Logger { get; }

        public FileLoggerProvider(string logFilePath, LogLevel logLevel = LogLevel.Information, long maxFileSize = 5242880, int maxRetainedFiles = 5)
        {
            Logger = new FileLogger(logFilePath, logLevel, maxFileSize, maxRetainedFiles);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return Logger;
        }

        public void Dispose()
        {
        }
    }
}
