using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using PowerPositionService.Interfaces;
using PowerPositionService.Services;
using PowerPositionService.Workers;
using PowerPositionService.Models;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Serilog.Settings.Configuration;

namespace PowerPositionService;

public static class Program
{
    private const string AppName = "PowerPositionService";
    private const string LogOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    
    public static async Task<int> Main(string[] args)
    {
        // Configure logging first to catch startup errors
        Log.Logger = CreateBootstrapLogger();
        
        try
        {
            Log.Information("===== {AppName} Starting =====", AppName);
            
            var host = CreateHostBuilder(args).Build();
            
            Log.Information("Application configured. Starting the host...");
            await host.RunAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("===== {AppName} Stopped =====", AppName);
            await Log.CloseAndFlushAsync();
        }
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
                
                Log.Logger = CreateLogger(hostingContext.Configuration);
                
                // Log configuration values (sensitive values should not be logged in production)
                Log.Information("Application starting with environment: {Environment}", env.EnvironmentName);
            })
            .UseSerilog((hostingContext, loggerConfiguration) => 
                ConfigureSerilog(loggerConfiguration, hostingContext.Configuration))
            .ConfigureServices((hostContext, services) =>
            {
                // Configure and validate settings
                var appSettings = hostContext.Configuration.GetSection("AppSettings").Get<AppSettings>();
                if (appSettings == null)
                {
                    throw new InvalidOperationException("AppSettings configuration is missing or invalid");
                }
                
                services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
                
                // Register services
                services.AddSingleton<IFileManager, FileManager>();
                services.AddSingleton<IPositionCalculator, PositionCalculator>();
                
                // Register PowerServiceWrapper with the correct constructor
                services.AddSingleton<IPowerService>(sp => 
                    new PowerServiceWrapper(
                        sp.GetRequiredService<ILogger<PowerServiceWrapper>>()));
                
                // Register the worker service
                services.AddHostedService<PowerPositionWorker>();
                
                Log.Information("Services registered");
            });
    
    private static Serilog.ILogger CreateBootstrapLogger()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: LogOutputTemplate,
                theme: AnsiConsoleTheme.Code);
            
        return loggerConfig.CreateBootstrapLogger();
    }
    
    private static Serilog.ILogger CreateLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
            {
                SectionName = "Serilog"
            })
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", AppName)
            .WriteTo.Console(
                outputTemplate: LogOutputTemplate,
                theme: AnsiConsoleTheme.Code)
            .CreateLogger();
    }
    
    private static void ConfigureSerilog(LoggerConfiguration loggerConfig, IConfiguration configuration)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
            {
                SectionName = "Serilog"
            })
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", AppName)
            .WriteTo.Console(
                outputTemplate: LogOutputTemplate,
                theme: AnsiConsoleTheme.Code);
    }
}
