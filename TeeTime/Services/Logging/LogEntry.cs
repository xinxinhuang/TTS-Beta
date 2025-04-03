using System;

namespace TeeTime.Services.Logging
{
    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; } = string.Empty;
        public object[]? Params { get; set; }
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
        public string ApplicationName { get; set; } = "TeeTime";
        public string? Source { get; set; }
    }
} 