using Microsoft.Extensions.Configuration;
using System;

namespace TeeTime.Services.Logging
{
    public static class LoggerFactory
    {
        public static ILoggerService CreateLogger(string? source = null)
        {
            return new DefaultLoggerService(source);
        }

        public static ILoggerService CreateConfiguredLogger(IConfiguration configuration, string? source = null)
        {
            var logger = new DefaultLoggerService(source);
            
            // Configure based on environment
            var consoleTransport = new ConsoleLoggerTransport();
            
            // Get minimum log level from configuration
            if (configuration != null)
            {
                var logLevelString = configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";
                if (Enum.TryParse<LogLevel>(logLevelString, out var logLevel))
                {
                    consoleTransport.MinimumLogLevel = logLevel;
                }
            }
            
            logger.AddTransport(consoleTransport);
            return logger;
        }
    }
} 