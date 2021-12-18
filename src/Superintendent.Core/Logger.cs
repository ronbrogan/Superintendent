using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Superintendent.Core
{
    public static class SuperintendentLog
    {
        public static void UseLogger(ILogger logger)
        {
            Logger.UseLogger(logger);
        }
    }

    internal static class Logger
    {
        private static ILogger logger = new ConsoleLogger();

        public static void UseLogger(ILogger? logger)
        {
            Logger.logger = logger ?? new ConsoleLogger();
        }

        private class ConsoleLogger : ILogger
        {
            private LoggerExternalScopeProvider scopes = new LoggerExternalScopeProvider();

            public IDisposable BeginScope<TState>(TState state)
            {
                return scopes.Push(state);
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var scope = new StringBuilder();

                scopes.ForEachScope((s,b) => {
                    if(b.Length > 0)
                        b.Append(">");
                    b.Append(s.ToString());
                }, scope);

                Console.WriteLine($"[{logLevel}] {(scope.Length > 0 ? ("<" + scope.ToString() + ">") : "")} {formatter(state, exception)}");
            }
        }

        //
        // Summary:
        //     Formats and writes a debug log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a debug log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogDebug(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes a debug log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogDebug(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a debug log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogDebug(string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, message, args);
        }

        //
        // Summary:
        //     Formats and writes a trace log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Trace, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a trace log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogTrace(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Trace, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes a trace log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogTrace(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Trace, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a trace log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogTrace(string message, params object[] args)
        {
            logger.Log(LogLevel.Trace, message, args);
        }

        //
        // Summary:
        //     Formats and writes an informational log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Information, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes an informational log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogInformation(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Information, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes an informational log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogInformation(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Information, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes an informational log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogInformation(string message, params object[] args)
        {
            logger.Log(LogLevel.Information, message, args);
        }

        //
        // Summary:
        //     Formats and writes a warning log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Warning, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a warning log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogWarning(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Warning, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes a warning log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogWarning(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Warning, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a warning log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogWarning(string message, params object[] args)
        {
            logger.Log(LogLevel.Warning, message, args);
        }

        //
        // Summary:
        //     Formats and writes an error log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogError(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Error, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes an error log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogError(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Error, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes an error log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogError(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Error, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes an error log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogError(string message, params object[] args)
        {
            logger.Log(LogLevel.Error, message, args);
        }

        //
        // Summary:
        //     Formats and writes a critical log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Critical, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a critical log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogCritical(EventId eventId, string message, params object[] args)
        {
            logger.Log(LogLevel.Critical, eventId, message, args);
        }

        //
        // Summary:
        //     Formats and writes a critical log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogCritical(Exception exception, string message, params object[] args)
        {
            logger.Log(LogLevel.Critical, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a critical log message.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   message:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void LogCritical(string message, params object[] args)
        {
            logger.Log(LogLevel.Critical, message, args);
        }

        //
        // Summary:
        //     Formats and writes a log message at the specified log level.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   logLevel:
        //     Entry will be written on this level.
        //
        //   message:
        //     Format string of the log message.
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void Log(LogLevel logLevel, string message, params object[] args)
        {
            logger.Log(logLevel, 0, null, message, args);
        }

        //
        // Summary:
        //     Formats and writes a log message at the specified log level.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   logLevel:
        //     Entry will be written on this level.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   message:
        //     Format string of the log message.
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void Log(LogLevel logLevel, EventId eventId, string message, params object[] args)
        {
            logger.Log(logLevel, eventId, null, message, args);
        }

        //
        // Summary:
        //     Formats and writes a log message at the specified log level.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   logLevel:
        //     Entry will be written on this level.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message.
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {
            logger.Log(logLevel, 0, exception, message, args);
        }

        //
        // Summary:
        //     Formats and writes a log message at the specified log level.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to write to.
        //
        //   logLevel:
        //     Entry will be written on this level.
        //
        //   eventId:
        //     The event id associated with the log.
        //
        //   exception:
        //     The exception to log.
        //
        //   message:
        //     Format string of the log message.
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        public static void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
        {
            logger.Log(logLevel, eventId, exception, message, args);
        }

        //
        // Summary:
        //     Formats the message and creates a scope.
        //
        // Parameters:
        //   logger:
        //     The Microsoft.Extensions.Logging.ILogger to create the scope in.
        //
        //   messageFormat:
        //     Format string of the log message in message template format. Example:
        //     "User {User} logged in from {Address}"
        //
        //   args:
        //     An object array that contains zero or more objects to format.
        //
        // Returns:
        //     A disposable scope object. Can be null.
        public static IDisposable BeginScope(string messageFormat, params object[] args)
        {
            return logger.BeginScope(messageFormat, args);
        }
    }
}
