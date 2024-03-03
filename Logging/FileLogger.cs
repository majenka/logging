using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;

namespace Majenka.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string categoryName;
        private readonly string logFileDirectory;
        private FileLoggerOptions options;

        private static readonly object queueLock = new object();
        private static readonly Queue<string> logQueue = new Queue<string>();
        
        private readonly System.Threading.Timer timer;

        private bool isRunning = true;
        private bool disposed = false;

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            this.categoryName = categoryName;
            this.options = options;

            if (options.MaxFileSize <= 0) throw new ArgumentException("MaxFileSize must be greater than 0");
            if (options.MaxRetainedFiles < 0) throw new ArgumentException("MaxRetainedFiles cannot less than 0");
            if (options.FlushInterval <= 0) throw new ArgumentException("FlushInterval must be greater than 0");
            if (options.Path == null) throw new ArgumentException("Path cannot be empty");

            logFileDirectory = Path.GetDirectoryName(options.Path) ?? throw new ArgumentException("Path is invalid");

            Directory.CreateDirectory(logFileDirectory);

            timer = new System.Threading.Timer(TimerCallback, null, options.FlushInterval, Timeout.Infinite);
        }

        public Boolean IsEnabled(LogLevel logLevel)
        {
            return logLevel >= options.MinLogLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return default;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!isRunning)
            {
                return;
            }

            if (options.Path == null)
            {
                return;
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var sb = new StringBuilder();

            if (options.LogDate)
            {
                sb.Append(DateTime.Now.ToString(options.DateFormat));
                sb.Append($"\t");
            }

            sb.Append($"[{logLevel}]:\t");

            if (categoryName != null)
            {
                sb.Append($"{categoryName}\t");
            }

            sb.Append(formatter(state, exception));

            lock (queueLock)
            {
                logQueue.Enqueue(sb.ToString());

                if (exception != null)
                {
                    logQueue.Enqueue("Stack trace:");
                }

                while (exception != null)
                {
                    logQueue.Enqueue($"\t{exception.GetType()}:\t{exception.Message}");

                    if (exception.StackTrace is string stackTrace)
                    {
                        logQueue.Enqueue($"\t\t{stackTrace}");
                    }

                    exception = exception.InnerException;
                }
            }
        }

        private void RollFile(string fileToRoll, int toFileNumber)
        {
            var rollFilePath = $"{options.Path}.{toFileNumber}";

            if (File.Exists(rollFilePath))
            {
                if (toFileNumber == options.MaxRetainedFiles)
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

        private void TimerCallback(object? state)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            lock (queueLock)
            {
                TryFlushBuffer();
            }

            timer.Change(options.FlushInterval, options.FlushInterval);
        }

        private void TryFlushBuffer()
        {
            try
            {
                while (logQueue.Count > 0)
                {
                    WriteLog(logQueue.Dequeue());
                }
            }
            catch (IOException ex)
            {
                // File may be locked by another process
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private void WriteLog(string logMessage)
        {
            using (StreamWriter writer = File.AppendText(options.Path))
            {
                writer.WriteLine(logMessage);
            }

            if (options.MaxFileSize <= new FileInfo(options.Path).Length)
            {
                RollFile(options.Path, 1);
            }
        }

        /// <summary>
        /// Flush log buffer and stop logging.
        /// </summary>
        public void FlushLog()
        {
            if (isRunning)
            {
                isRunning = false;
                TryFlushBuffer();
            }
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
                    timer.Dispose();
                    FlushLog();
                }

                disposed = true;
            }
        }
    }
}
