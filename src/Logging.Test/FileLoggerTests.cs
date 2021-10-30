using Majenka.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Logging.Test
{
    public class FileLoggerTests
    {
        private IHost CreateHost(string logFileName, LogLevel globalLogLevel, LogLevel providerLogLevel, long maxFileSize = 5242880)
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                   .ConfigureLogging((hostContext, builder) =>
                   {
                       builder.ClearProviders();
                       builder.SetMinimumLevel(globalLogLevel);
                       builder.AddFile(logFileName, logLevel: providerLogLevel, maxFileSize: maxFileSize, logDate: false);
                   });

            return hostBuilder.Build();
        }

        private string GetActualPath(string filename)
        {
            return Path.Combine("../../../", "Actual", filename);
        }

        private string GetExpectedPath(string actualPath)
        {
            return actualPath.Replace("Actual", "Expected");
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

            var host = CreateHost(logFileName, LogLevel.Trace, LogLevel.Trace);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestTraceWarning()
        {
            var logFileName = GetActualPath("trace_warning.txt");
            DeleteLogFile(logFileName);

            var host = CreateHost(logFileName, LogLevel.Trace, LogLevel.Warning);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestWarningTrace()
        {
            var logFileName = GetActualPath("warning_trace.txt");
            DeleteLogFile(logFileName);

            var host = CreateHost(logFileName, LogLevel.Warning, LogLevel.Trace);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestTraceInformation()
        {
            var logFileName = GetActualPath("trace_information.txt");
            DeleteLogFile(logFileName);

            var host = CreateHost(logFileName, LogLevel.Trace, LogLevel.Information);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            LogEach(logger);

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }

        [Test]
        public void TestFileLock()
        {
            var logFileName = GetActualPath("file_lock.txt");
            DeleteLogFile(logFileName);

            Parallel.For(0, 100, (id) =>
            {
                var host = CreateHost(logFileName, LogLevel.Trace, LogLevel.Information);
                var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

                for (int i = 0; i < 100; i++)
                {
                    logger.LogInformation($"{id}\t{i}");
                }
            });

            Assert.AreEqual(new FileInfo(GetExpectedPath(logFileName)).Length, new FileInfo(logFileName).Length);
        }

        [Test]
        public void TestRollOver()
        {
            var logFileName = GetActualPath("roll.txt"); 
            DeleteLogFile(logFileName);

            var host = CreateHost(logFileName, LogLevel.Information, LogLevel.Trace, 1000);
            var logger = host.Services.GetRequiredService<ILogger<FileLoggerTests>>();

            for (int i = 0; i < 100; i++)
            {
                logger.LogInformation($"Testing file roll over {i}");
            }

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

            var host = CreateHost(logFileName, LogLevel.Information, LogLevel.Trace, 1000);

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

            FileAssert.AreEqual(GetExpectedPath(logFileName), logFileName);
        }
    }
}