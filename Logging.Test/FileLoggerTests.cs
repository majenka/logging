using Majenka.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Logging.Test
{
    public class FileLoggerTests
    {
        private IHost CreateHost(FileLoggerProvider fileLoggerProvider, LogLevel globalLogLevel)
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                   .ConfigureLogging((hostContext, builder) =>
                   {
                       builder.ClearProviders();
                       builder.SetMinimumLevel(globalLogLevel);
                       builder.AddProvider(fileLoggerProvider);
                   });

            return hostBuilder.Build();
        }

        private static FileLoggerOptions CreateFileLoggerOptions(string logFileName, LogLevel minLogLevel,
            int maxRetainedFiles = 5,
            long maxFileSize = 5242880,
            bool logDate = false)
        {
            return new FileLoggerOptions
            {
                LogDate = logDate,
                MaxFileSize = maxFileSize,
                MaxRetainedFiles = maxRetainedFiles,
                MinLogLevel = minLogLevel,
                Path = logFileName
            };
        }

        private string GetActualPath(string filename)
        {
            return Path.Combine("../../../", "Actual", filename);
        }

        private string GetExpectedPath(string actualPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return actualPath.Replace("Actual", "Expected");
            }
            else
            {
                return actualPath.Replace("Actual", "Expected.Lx");
            }
        }

        private void DeleteLogFile(string logFileName)
        {
            if (File.Exists(logFileName))
            {
                File.Delete(logFileName);
            }
        }

        private static void LogEach(ILogger<FileLoggerTests> logger)
        {
            logger.LogTrace("Trace");
            logger.LogDebug("Debug");
            logger.LogInformation("Information");
            logger.LogWarning("Warning");
            logger.LogError("Error");
        }

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestTraceTrace()
        {
            var logFileName = GetActualPath("trace_trace.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Trace);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Trace);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);
            provider.FlushLoggers();

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }


        [Test]
        public void TestTraceWarning()
        {
            var logFileName = GetActualPath("trace_warning.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Warning);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Trace);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);
            provider.FlushLoggers();

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestWarningTrace()
        {
            var logFileName = GetActualPath("warning_trace.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Trace);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Warning);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);
            provider.FlushLoggers();

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestTraceInformation()
        {
            var logFileName = GetActualPath("trace_information.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Information);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Trace);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);
            provider.FlushLoggers();

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestFileLock()
        {
            var logFileName = GetActualPath("file_lock.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Information);

            var provider = new FileLoggerProvider(fileLoggerOptions);

            Parallel.For(0, 100, (id) =>
            {                
                var host = CreateHost(provider, LogLevel.Trace);
                var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

                for (int i = 0; i < 100; i++)
                {
                    logger.LogInformation($"{id}\t{i}");
                }
            });

            provider.FlushLoggers();

            Assert.AreEqual(new FileInfo(GetExpectedPath(logFileName)).Length, new FileInfo(logFileName).Length);
        }

        [Test]
        public void TestRollOver1()
        {
            var logFileName = GetActualPath("roll1.txt");
            DeleteLogFile(logFileName);
            DeleteLogFile($"{logFileName}.1");
            DeleteLogFile($"{logFileName}.2");
            DeleteLogFile($"{logFileName}.3");
            DeleteLogFile($"{logFileName}.4");
            DeleteLogFile($"{logFileName}.5");
            DeleteLogFile($"{logFileName}.6");

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Information, 1, 1000);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Information);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            for (int i = 0; i < 100; i++)
            {
                logger.LogInformation($"Testing file roll over {i}");
            }

            provider.FlushLoggers();

            var expectedFileName = GetExpectedPath(logFileName);

            FileAssert.AreEqual(expectedFileName, logFileName);
            FileAssert.AreEqual(expectedFileName + ".1", logFileName + ".1");
            FileAssert.DoesNotExist(logFileName + ".2");
            FileAssert.DoesNotExist(logFileName + ".3");
            FileAssert.DoesNotExist(logFileName + ".4");
            FileAssert.DoesNotExist(logFileName + ".5");
            FileAssert.DoesNotExist(logFileName + ".6");
        }

        [Test]
        public void TestRollOver5()
        {
            var logFileName = GetActualPath("roll5.txt");
            DeleteLogFile(logFileName);
            DeleteLogFile($"{logFileName}.1");
            DeleteLogFile($"{logFileName}.2");
            DeleteLogFile($"{logFileName}.3");
            DeleteLogFile($"{logFileName}.4");
            DeleteLogFile($"{logFileName}.5");
            DeleteLogFile($"{logFileName}.6");

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Information, 5, 1000);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Information);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            for (int i = 0; i < 100; i++)
            {
                logger.LogInformation($"Testing file roll over {i}");
            }

            provider.FlushLoggers();

            var expectedFileName = GetExpectedPath(logFileName);

            FileAssert.AreEqual(expectedFileName, logFileName);
            FileAssert.AreEqual(expectedFileName + ".1", logFileName + ".1");
            FileAssert.AreEqual(expectedFileName + ".2", logFileName + ".2");
            FileAssert.AreEqual(expectedFileName + ".3", logFileName + ".3");
            FileAssert.AreEqual(expectedFileName + ".4", logFileName + ".4");
            FileAssert.AreEqual(expectedFileName + ".5", logFileName + ".5");
            FileAssert.DoesNotExist(logFileName + ".6");
        }

        [Test]
        public void TestStackTrace()
        {
            var logFileName = GetActualPath("stacktrace.txt");
            DeleteLogFile(logFileName);

            FileLoggerOptions fileLoggerOptions = CreateFileLoggerOptions(logFileName, LogLevel.Information, 5, 1000);

            var provider = new FileLoggerProvider(fileLoggerOptions);
            var host = CreateHost(provider, LogLevel.Information);

            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();
            try
            {
                try
                {
                    int foo = 0, bar = 5;
                    logger.LogDebug($"When foo is {foo} and bar is {bar}, the number of foos in bar is:");
                    logger.LogDebug($"{bar / foo}");
                }
                catch (DivideByZeroException ex)
                {
                    throw new ArgumentException("Something went wrong calculating the number of foos in bar. See the inner exception for details.", ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred.");
            }

            provider.FlushLoggers();

            FileAssert.Exists(logFileName);
        }
    }
}