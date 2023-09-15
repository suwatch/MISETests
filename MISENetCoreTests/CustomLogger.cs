using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace JwtBearerHandlerApi
{
    // ILogger interface is implemented for demo purposes only. 
    public class CustomLogger : ILogger
    {
        public LogLevel MinLogLevel = LogLevel.Information;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= MinLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(formatter(state, exception), logLevel);
        }

        private void Log(string message, LogLevel logLevel)
        {
            Console.WriteLine($"{DateTime.Now} - {logLevel} - {message}");
        }

        public IDisposable BeginScope<TState>(TState state) => default;
    }
}
