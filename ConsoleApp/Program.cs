using ConsoleApp;
using Majenka.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;


FileLoggerProvider? loggerProvider = null;

// Create host for console app
IHostBuilder hostbuilder = Host.CreateDefaultBuilder(args);

// Add configuration files  
hostbuilder.ConfigureAppConfiguration((hostContext, builder) =>
{
    builder.Sources.Clear();

    builder.AddJsonFile("appsettings.json", false, true)
        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true, true)
        .AddEnvironmentVariables();
});

// Configure logging providers
hostbuilder.ConfigureLogging((hostContext, builder) =>
{
    var logLevel = hostContext.Configuration.GetValue<LogLevel>("Logging:LogLevel:Default");
    var options = hostContext.Configuration.GetSection("Logging:File").Get<FileLoggerOptions>() ?? throw new ArgumentNullException(nameof(FileLoggerOptions));

    loggerProvider = new FileLoggerProvider(options);

    builder.ClearProviders()
        .SetMinimumLevel(logLevel)
        .AddDebug()
        .AddConsole()
        .AddProvider(loggerProvider);

});

// Configure services
hostbuilder.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton<Service>();
});

// Build host and run console app
using (var host = hostbuilder.Build())
{
    var service = host.Services.GetRequiredService<Service>();

    service.Run();

    // Flush loggers and stop logging
    loggerProvider?.FlushLoggers();
}
       