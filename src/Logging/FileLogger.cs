using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Majenka.Logging
{
    public class FileLogger : ILogger
    {
        private string logFileDirectory;
        private long maxFileSize;
        private int maxRetainedFiles;
        private string logFilePath;
        private LogLevel minLogLevel;
        private FileInfo fileInfo;

        public FileLogger(string logFilePath, LogLevel minLogLevel, long maxFileSize, int maxRetainedFiles)
        {
            logFileDirectory = Path.GetDirectoryName(logFilePath) ?? throw new ArgumentException("logFilePath is empty or invalid");
            if (maxFileSize == 0) throw new ArgumentException("maxFileSize cannot be 0");

            this.maxFileSize = maxFileSize;
            this.maxRetainedFiles = maxRetainedFiles;
            this.logFilePath = logFilePath;
            this.minLogLevel = minLogLevel;

            Directory.CreateDirectory(logFileDirectory);
            fileInfo = new FileInfo(this.logFilePath);
        }

        public Boolean IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minLogLevel;
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

            if (maxRetainedFiles == 0 && maxFileSize > 0 && File.Exists(logFilePath) && fileInfo.Length > maxFileSize)
            {
                return;
            }

            using (var streamWriter = File.AppendText(logFilePath))
            {
                streamWriter.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                streamWriter.Write($"\t[{logLevel}]:\t");
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
            }

            if (maxRetainedFiles > 0 && maxFileSize <= new FileInfo(logFilePath).Length)
            {
                RollFile(logFilePath, 1);
            }
        }

        void RollFile(string fileToRoll, int toFileNumber)
        {
            var rollFilePath = $"{logFilePath}.{toFileNumber}";

            if (File.Exists(rollFilePath))
            {
                if(toFileNumber == maxRetainedFiles)
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
