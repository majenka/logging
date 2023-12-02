using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Majenka.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string categoryName;
        private readonly string logFileDirectory;
        private FileLoggerOptions options;
        private FileInfo fileInfo;

        private static readonly object queueLock = new object();
        private static readonly object fileLock = new object();

        private readonly Queue<string> logQueue = new Queue<string>();
        private readonly AutoResetEvent logEvent = new AutoResetEvent(false);
        private Thread workerThread;
        private bool disposed = false;
        private bool isRunning = true;

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            this.categoryName = categoryName;
            this.options = options;

            if (options.MaxFileSize <= 0) throw new ArgumentException("MaxFileSize must be greater than 0");
            if (options.MaxRetainedFiles < 0) throw new ArgumentException("MaxRetainedFiles cannot less than 0");
            if (options.Path == null) throw new ArgumentException("Path cannot be empty");

            logFileDirectory = Path.GetDirectoryName(options.Path) ?? throw new ArgumentException("Path is invalid");

            Directory.CreateDirectory(logFileDirectory);
            fileInfo = new FileInfo(options.Path);

            // Start the worker thread
            workerThread = new Thread(WorkerThread);
            workerThread.Start();
        }

        public Boolean IsEnabled(LogLevel logLevel)
        {
            return logLevel >= options.MinLogLevel;
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return default;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if(!isRunning)
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

            lock (queueLock)
            {
                var sb = new StringBuilder();

                if (options.LogDate)
                {
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                    sb.Append($"\t");
                }

                sb.Append($"[{logLevel}]:\t");

                if (categoryName != null)
                {
                    sb.Append($"{categoryName}\t");
                }

                sb.Append(formatter(state, exception));
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

                logEvent.Set(); // Signal the worker thread to process the log
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

        private void WorkerThread()
        {
            while (isRunning)
            {
                logEvent.WaitOne(); // Wait for a signal to process the log

                while (logQueue.Count > 0)
                {
                    string logMessage;

                    lock (queueLock)
                    {
                        logMessage = logQueue.Dequeue();
                    }

                    lock (fileLock)
                    {
                        WriteLog(logMessage);
                    }
                }
            }
        }

        private void WriteLog(string logMessage)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        private void FlushQueue()
        {
            if (isRunning)
            {
                isRunning = false;
                logEvent.Set(); // Signal the worker thread to exit
                workerThread.Join(); // Wait for the worker thread to finish
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
                    // Dispose managed resources (flush logs, close connections, etc.)
                    FlushQueue();
                }

                disposed = true;
            }
        }
    }
}
