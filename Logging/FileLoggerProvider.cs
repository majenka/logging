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

        public FileLoggerProvider(FileLoggerOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            loggers = new List<FileLogger>();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new FileLogger(categoryName, options);
            loggers.Add(logger);
            return logger;
        }

        public void FlushLoggers()
        {
            foreach (var logger in loggers)
            {
                logger.FlushLog();
            }
        }

        public void Dispose()
        {
            foreach (var logger in loggers)
            {
                logger.Dispose();
            }
        }
    }
}
