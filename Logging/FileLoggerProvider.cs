using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Majenka.Logging
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private FileLoggerOptions options { get; }

        private ICollection<FileLogger> loggers { get; }

        private bool disposed = false;

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

            loggers = new List<FileLogger>();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new FileLogger(categoryName, options);
            loggers.Add(logger);
            return logger;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var logger in loggers)
                    {
                        logger.Dispose();
                    }
                }

                disposed = true;
            }
        }
    }
}
