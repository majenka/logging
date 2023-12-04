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
    public class FileLogger : ILogger
    {
        private readonly string categoryName;
        private readonly string logFileDirectory;
        private FileLoggerOptions options;

        private static readonly object queueLock = new object();
        private static readonly Queue<string> logQueue = new Queue<string>();
        
        private readonly AutoResetEvent logEvent = new AutoResetEvent(false);
        private readonly Thread workerThread;

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

            if (logQueue.Count > options.BufferLines - 1)
            {
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

                lock (queueLock)
                {
                    //WriteLog($"******* {logQueue.Count} **********");

                    while (logQueue.Count > 0)
                    {
                        WriteLog(logQueue.Dequeue());
                    }
                }
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

        public void FlushLog()
        {
            if (isRunning)
            {
                isRunning = false;
                logEvent.Set(); // Signal the worker thread to exit
                workerThread.Join(); // Wait for the worker thread to finish
            }
        }
    }
}
