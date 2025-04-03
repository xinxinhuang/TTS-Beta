using System;
using System.Collections.Generic;

namespace TeeTime.Services.Logging
{
    public class DefaultLoggerService : ILoggerService
    {
        private readonly List<ILoggerTransport> _transports = new();
        private string? _source;
        private string? _correlationId;

        public DefaultLoggerService(string? source = null)
        {
            _source = source;
            // Add console transport by default
            _transports.Add(new ConsoleLoggerTransport());
        }

        public void AddTransport(ILoggerTransport transport)
        {
            _transports.Add(transport);
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId;
        }

        private void Log(LogLevel level, string message, Exception? exception = null, params object[] args)
        {
            if (!IsEnabled(level))
                return;

            var entry = new LogEntry
            {
                LogLevel = level,
                Message = message,
                Params = args,
                Exception = exception,
                Source = _source,
                CorrelationId = _correlationId
            };

            foreach (var transport in _transports)
            {
                transport.Send(entry);
            }
        }

        public void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, null, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(LogLevel.Information, message, null, args);
        }

        public void Warn(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, null, args);
        }

        public void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, null, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            Log(LogLevel.Error, message, exception, args);
        }

        public void Critical(string message, params object[] args)
        {
            Log(LogLevel.Critical, message, null, args);
        }

        public void Critical(Exception exception, string message, params object[] args)
        {
            Log(LogLevel.Critical, message, exception, args);
        }

        public bool IsEnabled(LogLevel level)
        {
            foreach (var transport in _transports)
            {
                if (transport.IsEnabled(level))
                    return true;
            }
            return false;
        }
    }
} 