using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Majenka.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string categoryName;
        private readonly string logFileDirectory;
        private FileLoggerOptions options;
        private FileInfo fileInfo;

        private static readonly object criticalSection = new object();

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            this.categoryName = categoryName;
            this.options = options;

            if (options.MaxFileSize <= 0) throw new ArgumentException("MaxFileSize must be greater than 0");
            if (options.MaxRetainedFiles < 0) throw new ArgumentException("MaxRetainedFiles cannot less than 0");
            if (options.Path == null ) throw new ArgumentException("Path cannot be empty");

            logFileDirectory = Path.GetDirectoryName(options.Path) ?? throw new ArgumentException("Path is invalid");

            Directory.CreateDirectory(logFileDirectory);
            fileInfo = new FileInfo(options.Path);
        }

        public Boolean IsEnabled(LogLevel logLevel)
        {
            return logLevel >= options.MinLogLevel;
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (options.MaxRetainedFiles == 0 && File.Exists(options.Path) && fileInfo.Length > options.MaxFileSize)
            {
                return;
            }

            if (options.Path == null)
            {
                return;
            }

            lock (criticalSection)
            {
                using (var streamWriter = File.AppendText(options.Path))
                {
                    if (options.LogDate)
                    {
                        streamWriter.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                        streamWriter.Write($"\t");
                    }

                    streamWriter.Write($"[{logLevel}]:\t");

                    if (categoryName != null)
                    {
                        streamWriter.Write($"{categoryName}\t");
                    }

                    streamWriter.WriteLine(formatter(state, exception));

                    if (exception != null)
                    {
                        streamWriter.WriteLine("Stack trace:");
                    }

                    while (exception != null)
                    {
                        streamWriter.WriteLine($"\t{exception.GetType()}:\t{exception.Message}");

                        if (exception.StackTrace is string stackTrace)
                        {
                            streamWriter.WriteLine($"\t\t{stackTrace}");
                        }

                        exception = exception.InnerException;
                    }

                    streamWriter.Flush();
                }

                if (options.MaxRetainedFiles > 0 && options.MaxFileSize <= new FileInfo(options.Path).Length)
                {
                    RollFile(options.Path, 1);
                }
            }
        }
        
        void RollFile(string fileToRoll, int toFileNumber)
        {
            var rollFilePath = $"{options.Path}.{toFileNumber}";

            if (File.Exists(rollFilePath))
            {
                if(toFileNumber == options.MaxRetainedFiles)
                {
                    File.Delete(rollFilePath);
                }
                else
                {
                    RollFile(rollFilePath, ++toFileNumber);
                }
            }

            File.Move(fileToRoll, rollFilePath);
        }
    }
}
