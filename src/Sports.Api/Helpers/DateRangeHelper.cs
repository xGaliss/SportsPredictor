namespace Sports.Api.Helpers;

public static class DateRangeHelper
{
    private const string DefaultTimeZoneId = "Europe/Madrid";

    public static DateOnly GetCurrentLocalDate()
    {
        var tz = ResolveTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(localNow);
    }

    public static (DateTime startUtc, DateTime endUtc) GetUtcRangeForLocalDate(DateOnly localDate)
    {
        var tz = ResolveTimeZone();
        var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        return (startUtc, endUtc);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
