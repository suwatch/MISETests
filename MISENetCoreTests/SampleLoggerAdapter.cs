using Microsoft.Extensions.Logging;
using Microsoft.Identity.ServiceEssentials;
using Microsoft.IdentityModel.Abstractions;

namespace JwtBearerHandlerApi
{
    public class SampleLoggerAdapter : IMiseLogger, IIdentityLogger
    {
        private readonly CustomLogger _customLogger = null;

        public SampleLoggerAdapter(CustomLogger customLogger)
        {
            _customLogger = customLogger;
        }

        public bool IsEnabled(LogSeverityLevel logSeverityLevel)
        {
            return ConvertToLogLevel(logSeverityLevel) >= _customLogger.MinLogLevel;
        }

        private LogLevel ConvertToLogLevel(LogSeverityLevel severityLevel)
        {
            switch (severityLevel)
            {
                case LogSeverityLevel.Trace: return LogLevel.Trace;
                case LogSeverityLevel.Debug: return LogLevel.Debug;
                case LogSeverityLevel.Information: return LogLevel.Information;
                case LogSeverityLevel.Warning: return LogLevel.Warning;
                case LogSeverityLevel.Error: return LogLevel.Error;
                case LogSeverityLevel.Critical: return LogLevel.Critical;
                default: return LogLevel.None;
            }
        }

        public void Log(string message, LogSeverityLevel severityLevel)
        {
            switch (severityLevel)
            {
                case LogSeverityLevel.Trace:
                    _customLogger.LogTrace(message);
                    break;

                case LogSeverityLevel.Debug:
                    _customLogger.LogDebug(message);
                    break;

                case LogSeverityLevel.Information:
                    _customLogger.LogInformation(message);
                    break;

                case LogSeverityLevel.Warning:
                    _customLogger.LogWarning(message);
                    break;

                case LogSeverityLevel.Error:
                    _customLogger.LogError(message);
                    break;

                case LogSeverityLevel.Critical:
                    _customLogger.LogCritical(message);
                    break;

                default:
                    break;
            }
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return ConvertToLogLevel(eventLogLevel) >= _customLogger.MinLogLevel;
        }

        public LogLevel ConvertToLogLevel(EventLogLevel eventLogLevel)
        {
            switch (eventLogLevel)
            {
                case EventLogLevel.LogAlways: return LogLevel.Trace;
                case EventLogLevel.Verbose: return LogLevel.Debug;
                case EventLogLevel.Informational: return LogLevel.Information;
                case EventLogLevel.Warning: return LogLevel.Warning;
                case EventLogLevel.Error: return LogLevel.Error;
                case EventLogLevel.Critical: return LogLevel.Critical;
                default: return LogLevel.Debug;
            }
        }

        public void Log(LogEntry entry)
        {
            if (entry != null)
            {
                switch (entry.EventLogLevel)
                {
                    case EventLogLevel.LogAlways:
                        _customLogger.LogTrace(entry.Message);
                        break;

                    case EventLogLevel.Verbose:
                        _customLogger.LogDebug(entry.Message);
                        break;

                    case EventLogLevel.Informational:
                        _customLogger.LogInformation(entry.Message);
                        break;

                    case EventLogLevel.Warning:
                        _customLogger.LogWarning(entry.Message);
                        break;

                    case EventLogLevel.Error:
                        _customLogger.LogError(entry.Message);
                        break;

                    case EventLogLevel.Critical:
                        _customLogger.LogCritical(entry.Message);
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
