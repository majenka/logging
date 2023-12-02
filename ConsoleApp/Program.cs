using Majenka.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main()
        {
            FileLoggerProvider? loggerProvider = null;

            var hostbuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    builder.Sources.Clear();

                    builder.AddJsonFile("appsettings.json", false, true)
                           .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true, true)
                           .AddEnvironmentVariables();
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    var logLevel = hostContext.Configuration.GetValue<LogLevel>("Logging:LogLevel:Default");
                    var options = hostContext.Configuration.GetSection("Logging:File").Get<FileLoggerOptions>();

                    loggerProvider = new FileLoggerProvider(
                        options.Path, 
                        logLevel, 
                        options.MaxFileSize, 
                        options.MaxRetainedFiles, 
                        options.LogDate);

                    builder.ClearProviders()
                        .SetMinimumLevel(logLevel)
                        .AddDebug()
                        .AddConsole()
                        .AddProvider(loggerProvider);

                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<Service>();
                });

            using (var host = hostbuilder.Build())
            {
                var logService = host.Services.GetRequiredService<Service>();
                logService.Run();
                loggerProvider?.Dispose();
            }
        }
    }
}
