using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Majenka.Logging
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private FileLoggerOptions options { get; }
            
        public FileLoggerProvider(string logFilePath, LogLevel logLevel, long maxFileSize, int maxRetainedFiles, bool logDate)
        {
            options = new FileLoggerOptions
            {
                Path = logFilePath,
                MinLogLevel = logLevel,
                MaxFileSize = maxFileSize,
                MaxRetainedFiles = maxRetainedFiles,
                LogDate = logDate
            };
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, options);
        }

        public void Dispose()
        {
        }
    }
}
