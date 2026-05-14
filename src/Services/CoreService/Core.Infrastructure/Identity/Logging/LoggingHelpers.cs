using System;
using System.Collections.Generic;
using Core.Application.Identity.Common.Interfaces;

namespace Core.Infrastructure.Identity.Logging;

/// <summary>
/// Sensitive data masking helper to prevent logging of passwords, tokens, and personal data.
/// </summary>
public static class SensitiveDataMasker
{
    private static readonly HashSet<string> SensitiveKeyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "token",
        "secret",
        "apikey",
        "api-key",
        "authorization",
        "jwt",
        "bearer",
        "credential",
        "privatekey",
        "private-key",
        "enckey",
        "enc-key",
        "jwtkey",
        "jwt-key",
        "securitykey",
        "security-key",
        "refreshtoken",
        "refresh-token",
        "accesstoken",
        "access-token",
        "otp",
        "mfa",
        "connectionstring",
        "connection-string"
    };

    /// <summary>
    /// Masks sensitive values in a property dictionary.
    /// Returns a new dictionary with masked values for sensitive keys.
    /// </summary>
    public static Dictionary<string, object> MaskSensitiveData(Dictionary<string, object>? properties)
    {
        if (properties == null || properties.Count == 0)
            return new Dictionary<string, object>();

        var masked = new Dictionary<string, object>();

        foreach (var kvp in properties)
        {
            if (IsSensitiveKey(kvp.Key))
            {
                masked[kvp.Key] = "***MASKED***";
            }
            else if (kvp.Value is string stringValue && IsSensitiveValue(kvp.Key, stringValue))
            {
                masked[kvp.Key] = "***MASKED***";
            }
            else
            {
                masked[kvp.Key] = kvp.Value;
            }
        }

        return masked;
    }

    /// <summary>
    /// Masks a sensitive value.
    /// </summary>
    public static string MaskValue(string? value, bool forceRedact = false)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (forceRedact)
            return "***MASKED***";

        // Mask JWT tokens (starts with "eyJ")
        if (value.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase))
            return "***JWT_TOKEN_MASKED***";

        // Mask long tokens/keys (more than 50 chars that look like credentials)
        if (value.Length > 50 && !value.Contains(" ", StringComparison.Ordinal))
            return "***TOKEN_MASKED***";

        return value;
    }

    private static bool IsSensitiveKey(string key)
    {
        foreach (var pattern in SensitiveKeyPatterns)
        {
            if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsSensitiveValue(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Check if value looks like a JWT token
        if (value.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if it looks like a Bearer token (long alphanumeric string)
        if (value.Length > 100 && IsLikelyToken(value))
            return true;

        return false;
    }

    private static bool IsLikelyToken(string value)
    {
        // Token-like: mostly alphanumeric with hyphens/underscores, no spaces
        if (value.Contains(" ", StringComparison.Ordinal))
            return false;

        int alphanumericCount = 0;
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                alphanumericCount++;
        }

        return alphanumericCount > (value.Length * 0.8);
    }
}

/// <summary>
/// Performance monitoring helper for tracking operation durations.
/// </summary>
public sealed class PerformanceTimer : IDisposable
{
    private readonly string _operationName;
    private readonly IStructuredLogger _logger;
    private readonly Dictionary<string, object> _additionalProperties;
    private readonly long _slowThresholdMs;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    public PerformanceTimer(
        string operationName,
        IStructuredLogger logger,
        long slowThresholdMs = 1000,
        Dictionary<string, object>? additionalProperties = null)
    {
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _additionalProperties = additionalProperties ?? new Dictionary<string, object>();
        _slowThresholdMs = slowThresholdMs;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var durationMs = _stopwatch.ElapsedMilliseconds;

        if (durationMs > _slowThresholdMs)
        {
            _logger.LogSlowOperation(_operationName, durationMs, _slowThresholdMs, _additionalProperties);
        }
        else
        {
            _logger.LogOperationWithMetrics(_operationName, durationMs, true, _additionalProperties);
        }
    }
}
