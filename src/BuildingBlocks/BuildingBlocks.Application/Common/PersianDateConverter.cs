using System.Globalization;

namespace BuildingBlocks.Application.Common;

/// <summary>
/// Parses Gregorian (ISO) and Jalali (Persian) date strings for range filters.
/// </summary>
public static class PersianDateConverter
{
    private static readonly PersianCalendar Calendar = new();

    public static DateTimeOffset? TryParseRangeStart(string? input)
    {
        if (!TryParse(input, out var date))
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc));
    }

    public static DateTimeOffset? TryParseRangeEnd(string? input)
    {
        if (!TryParse(input, out var date))
            return null;

        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        return new DateTimeOffset(DateTime.SpecifyKind(endOfDay, DateTimeKind.Utc));
    }

    public static bool TryParse(string? input, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        if (DateTimeOffset.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var gregorian))
        {
            date = gregorian.UtcDateTime;
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDate))
        {
            date = localDate;
            return true;
        }

        var separators = new[] { '/', '-', '.' };
        var parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
            return false;

        if (year is < 1200 or > 1600)
            return false;

        try
        {
            date = Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
