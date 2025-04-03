namespace TeeTime.Services.Logging
{
    public interface ILoggerTransport
    {
        void Send(LogEntry entry);
        LogLevel MinimumLogLevel { get; set; }
        bool IsEnabled(LogLevel level);
    }
} 