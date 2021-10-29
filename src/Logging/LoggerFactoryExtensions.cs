using Microsoft.Extensions.Logging;
using System;

namespace Majenka.Logging
{
    public static class LoggerFactoryExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath)
        {
            builder.AddProvider(new FileLoggerProvider(logFilePath));

            return builder;
        }

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath, LogLevel logLevel)
        {
            builder.AddProvider(new FileLoggerProvider(logFilePath, logLevel));

            return builder;
        }

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath, LogLevel logLevel, long maxFileSize)
        {
            builder.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize));

            return builder;
        }

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logFilePath, LogLevel logLevel, long maxFileSize, int maxRetainedFiles)
        {
            builder.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize, maxRetainedFiles));

            return builder;
        }

        public static ILoggerFactory AddFile(this ILoggerFactory factory, string logFilePath)
        {
            factory.AddProvider(new FileLoggerProvider(logFilePath));
            return factory;
        }

        public static ILoggerFactory AddFile(this ILoggerFactory factory, string logFilePath, LogLevel logLevel)
        {
            factory.AddProvider(new FileLoggerProvider(logFilePath, logLevel));
            return factory;
        }

        public static ILoggerFactory AddFile(this ILoggerFactory factory, string logFilePath, LogLevel logLevel, long maxFileSize)
        {
            factory.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize));
            return factory;
        }

        public static ILoggerFactory AddFile(this ILoggerFactory factory, string logFilePath, LogLevel logLevel, long maxFileSize, int maxRetainedFiles)
        {
            factory.AddProvider(new FileLoggerProvider(logFilePath, logLevel, maxFileSize, maxRetainedFiles));
            return factory;
        }
    }
}
