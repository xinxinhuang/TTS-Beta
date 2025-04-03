using System;

namespace TeeTime.Services.Logging
{
    public class ConsoleLoggerTransport : ILoggerTransport
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        public bool IsEnabled(LogLevel level)
        {
            return level >= MinimumLogLevel;
        }

        public void Send(LogEntry entry)
        {
            if (!IsEnabled(entry.LogLevel))
                return;

            string prefix = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.LogLevel}]";
            if (!string.IsNullOrEmpty(entry.Source))
                prefix += $" [{entry.Source}]";
            if (!string.IsNullOrEmpty(entry.CorrelationId))
                prefix += $" [{entry.CorrelationId}]";

            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColorForLogLevel(entry.LogLevel);

            Console.WriteLine($"{prefix}: {entry.Message}");
            
            if (entry.Params != null && entry.Params.Length > 0)
            {
                Console.WriteLine($"  Parameters: {string.Join(", ", entry.Params)}");
            }

            if (entry.Exception != null)
            {
                Console.WriteLine($"  Exception: {entry.Exception.GetType().Name}");
                Console.WriteLine($"  Message: {entry.Exception.Message}");
                Console.WriteLine($"  StackTrace: {entry.Exception.StackTrace}");
            }

            Console.ForegroundColor = originalColor;
        }

        private ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }
} 