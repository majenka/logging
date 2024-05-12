using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Majenka.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string categoryName;
        private readonly string logFileDirectory;
        private FileLoggerOptions options;

        private static readonly object queueLock = new object();
        private static readonly Queue<FileLoggerLine> logQueue = new Queue<FileLoggerLine>();
        private static Timer timer;
        private static int flushInterval = 3000; // milliseconds

        private bool isRunning = true;
        private bool disposed = false;

        static FileLogger()
        {
            timer = new Timer(TimerCallback, null, flushInterval, Timeout.Infinite);
        }

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            this.categoryName = categoryName;
            this.options = options;

            if (options.MaxFileSize < 3000) throw new ArgumentException("MaxFileSize cannot be less than 3000 bytes");  // 4kB block size allowing ~ 20% for metadata overhead
            if (options.MaxRetainedFiles < 0) throw new ArgumentException("MaxRetainedFiles cannot be less than 0");    // 0 indicates don't retain any files and just overwrite
            if (options.Path == null) throw new ArgumentException("Path cannot be empty");

            logFileDirectory = Path.GetDirectoryName(options.Path) ?? throw new ArgumentException("Path is invalid");

            Directory.CreateDirectory(logFileDirectory);
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
                logQueue.Enqueue(new FileLoggerLine(options, sb.ToString()));

                if (exception != null)
                {
                    logQueue.Enqueue(new FileLoggerLine(options, "Stack trace:"));
                }

                while (exception != null)
                {
                    logQueue.Enqueue(new FileLoggerLine(options, $"\t{exception.GetType()}:\t{exception.Message}"));

                    if (exception.StackTrace is string stackTrace)
                    {
                        logQueue.Enqueue(new FileLoggerLine(options, $"\t\t{stackTrace}"));
                    }

                    exception = exception.InnerException;
                }
            }
        }

        private static void RollFile(FileLoggerOptions options, string fileToRoll, int toFileNumber)
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
                    RollFile(options, rollFilePath, ++toFileNumber);
                }
            }

            File.Move(fileToRoll, rollFilePath);
        }

        private static void TimerCallback(object? state)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            lock (queueLock)
            {
                TryFlushBuffer();
            }

            timer.Change(flushInterval, Timeout.Infinite);
        }

        private static void TryFlushBuffer()
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

        private static void WriteLog(FileLoggerLine fileLoggerLine)
        {
            using (StreamWriter writer = File.AppendText(fileLoggerLine.Options.Path))
            {
                writer.WriteLine(fileLoggerLine.Message);
            }

            if (fileLoggerLine.Options.MaxFileSize <= new FileInfo(fileLoggerLine.Options.Path).Length)
            {
                RollFile(fileLoggerLine.Options, fileLoggerLine.Options.Path, 1);
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
                    FlushLog();
                }

                disposed = true;
            }
        }
    }
}
