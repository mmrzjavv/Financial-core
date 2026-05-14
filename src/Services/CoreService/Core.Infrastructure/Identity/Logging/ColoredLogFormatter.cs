using Serilog.Events;
using Serilog.Formatting;

namespace Core.Infrastructure.Identity.Logging
{
    public class ColoredLogFormatter : ITextFormatter
    {
        private const string ResetColor = "\x1b[0m";
        private const string Bold = "\x1b[1m";

        // Success colors (green)
        private const string SuccessColor = "\x1b[32m"; // Green
        private const string SuccessBoldColor = "\x1b[1;32m"; // Bold Green

        // Error colors (red)
        private const string ErrorColor = "\x1b[31m"; // Red
        private const string ErrorBoldColor = "\x1b[1;31m"; // Bold Red

        // Warning colors (yellow)
        private const string WarningColor = "\x1b[33m"; // Yellow
        private const string WarningBoldColor = "\x1b[1;33m"; // Bold Yellow

        // Info colors (blue)
        private const string InfoColor = "\x1b[34m"; // Blue
        private const string InfoBoldColor = "\x1b[1;34m"; // Bold Blue

        // Debug colors (cyan)
        private const string DebugColor = "\x1b[36m"; // Cyan
        private const string DebugBoldColor = "\x1b[1;36m"; // Bold Cyan

        // Security colors (magenta)
        private const string SecurityColor = "\x1b[35m"; // Magenta
        private const string SecurityBoldColor = "\x1b[1;35m"; // Bold Magenta

        // Performance colors (bright blue)
        private const string PerformanceColor = "\x1b[94m"; // Bright Blue
        private const string PerformanceBoldColor = "\x1b[1;94m"; // Bold Bright Blue

        public void Format(LogEvent logEvent, TextWriter output)
        {
            var timestamp = logEvent.Timestamp.ToString("HH:mm:ss");
            var level = logEvent.Level.ToString().ToUpper();
            var message = logEvent.RenderMessage();
            var exception = logEvent.Exception;

            var (levelColor, messageColor) = GetColors(logEvent.Level, message);

            output.Write($"{Bold}[{timestamp}]{ResetColor} ");
            output.Write($"{levelColor}?{ResetColor} ");
            output.Write($"{messageColor}{message}{ResetColor}");

            var relevantProperties = logEvent.Properties
                .Where(p => !IsExcludedProperty(p.Key))
                .ToList();

            if (relevantProperties.Any())
            {
                output.Write($" {InfoColor}|{ResetColor}");
                foreach (var property in relevantProperties)
                {
                    var value = property.Value.ToString().Trim('"');
                    output.Write($" {InfoColor}{property.Key}:{ResetColor} {value}");
                }
            }

            if (exception != null)
            {
                output.Write(
                    $"\n{ErrorBoldColor}  ?? Exception:{ResetColor} {ErrorColor}{exception.Message}{ResetColor}");
                if (exception.StackTrace != null)
                {
                    var stackTrace = exception.StackTrace.Split('\n').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        output.Write($"\n{ErrorColor}     at {stackTrace}{ResetColor}");
                    }
                }
            }

            output.WriteLine();
        }

        private static bool IsExcludedProperty(string propertyName)
        {
            var excludedProperties = new[]
            {
                "ConnectionId", "CorrelationId", "ClientIp", "RequestId", "RequestPath",
                "ApplicationName", "Environment", "MachineName", "ThreadId", "SourceContext",
                "LogMessage", "ErrorMessage", "RequestId", "RequestPath"
            };
            return excludedProperties.Contains(propertyName);
        }

        private (string levelColor, string messageColor) GetColors(LogEventLevel level, string message)
        {
            var messageLower = message.ToLower();

            if (messageLower.Contains("success") || messageLower.Contains("completed") ||
                messageLower.Contains("created") ||
                messageLower.Contains("updated") || messageLower.Contains("deleted") ||
                messageLower.Contains("retrieved") ||
                messageLower.Contains("established") || messageLower.Contains("initialized") ||
                messageLower.Contains("succeeded"))
            {
                return (SuccessBoldColor, SuccessColor);
            }

            // Check for ERROR patterns (including Arabic text patterns)
            if (messageLower.Contains("error") || messageLower.Contains("failed") ||
                messageLower.Contains("exception") ||
                messageLower.Contains("validation failure") || messageLower.Contains("business rule violation") ||
                messageLower.Contains("failure") || messageLower.Contains("timeout") ||
                messageLower.Contains("invalid"))
            {
                return (ErrorBoldColor, ErrorColor);
            }

            if (messageLower.Contains("warning") || messageLower.Contains("high memory") ||
                messageLower.Contains("slow") ||
                messageLower.Contains("detected") || messageLower.Contains("usage"))
            {
                return (WarningBoldColor, WarningColor);
            }

            if (messageLower.Contains("security") || messageLower.Contains("authentication") ||
                messageLower.Contains("authorization") ||
                messageLower.Contains("login") || messageLower.Contains("password") || messageLower.Contains("access"))
            {
                return (SecurityBoldColor, SecurityColor);
            }

            if (messageLower.Contains("performance") || messageLower.Contains("duration") ||
                messageLower.Contains("timing") ||
                messageLower.Contains("query") || messageLower.Contains("operation") ||
                messageLower.Contains("endpoint"))
            {
                return (PerformanceBoldColor, PerformanceColor);
            }

            if (messageLower.Contains("user activity") || messageLower.Contains("data access") ||
                messageLower.Contains("user:") ||
                messageLower.Contains("activity") || messageLower.Contains("accessed") ||
                messageLower.Contains("viewed"))
            {
                return (InfoBoldColor, InfoColor);
            }

            if (messageLower.Contains("system operation") || messageLower.Contains("application") ||
                messageLower.Contains("started") ||
                messageLower.Contains("connection") || messageLower.Contains("database"))
            {
                return (InfoBoldColor, InfoColor);
            }

            return level switch
            {
                LogEventLevel.Debug => (DebugBoldColor, DebugColor),
                LogEventLevel.Information => (InfoBoldColor, InfoColor),
                LogEventLevel.Warning => (WarningBoldColor, WarningColor),
                LogEventLevel.Error => (ErrorBoldColor, ErrorColor),
                LogEventLevel.Fatal => (ErrorBoldColor, ErrorColor),
                _ => (InfoBoldColor, InfoColor)
            };
        }
    }
}