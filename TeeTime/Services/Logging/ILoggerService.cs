using System;

namespace TeeTime.Services.Logging
{
    public interface ILoggerService
    {
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);
        void Error(Exception exception, string message, params object[] args);
        void Critical(string message, params object[] args);
        void Critical(Exception exception, string message, params object[] args);
        bool IsEnabled(LogLevel level);
        void SetCorrelationId(string correlationId);
    }
} 