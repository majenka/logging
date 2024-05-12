# logging
Logging library for dotnet 8

File Logger for dotnet core (C#)

Written for a console app with light logging so may not suited for other app types. 

Developed and tested on Windows 10 NTFS

Successfully used on Linux (Raspian, Debian)

Instructions for using FileLogger in a console app
--------------------------------------------------

Configure file logging in the appsettings.json file (create the appsettings.json file if it doesn't exist. MaxFileSize is in bytes).

    
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "Microsoft.Hosting.Lifetime": "Information"
        },
        "File": {
          "Path": "/var/log/console-app.log",
          "MaxFileSize": 5242880,
          "MaxRetainedFiles": 5,
          "DateFormat": "yyyy-MM-dd HH:mm:ss.fff",
          "LogDate": true
        }
      }
    

Configuring file logging in Program.cs
--------------------------------------
      
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
        
        // Configure services (E.g. if your code is in a single service called Service)
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
      
