using Microsoft.Extensions.Logging;
using System;

namespace Majenka.Logging
{
    public static class LoggerFactoryExtensions
    {

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath, LogLevel logLevel = LogLevel.Information, long maxFileSize = 5242880, int maxRetainedFiles = 5, bool logDate = true)
        {
            builder.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize, maxRetainedFiles, logDate));

            return builder;
        }

        public static ILoggerFactory AddFile(this ILoggerFactory factory, string logFilePath, LogLevel logLevel = LogLevel.Information, long maxFileSize = 5242880, int maxRetainedFiles = 5, bool logDate = true)
        {
            factory.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize, maxRetainedFiles, logDate));
            return factory;
        }
    }
}
