namespace SubscriptionApp.Infrastructure.Utilities;

/// <summary>
/// Single source of "today" for reminder, dashboard and auto-pay logic.
/// Uses Turkey time (UTC+3, no DST) so business-day math matches what the
/// customer sees on the wall clock, not UTC.
/// </summary>
public static class BusinessClock
{
    private static readonly TimeZoneInfo TurkeyTime = ResolveTurkeyTimeZone();

    public static DateTime Now() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTime);

    public static DateTime Today()
    {
        var now = Now();
        return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    public static string CurrentPeriod() => Now().ToString("yyyy-MM");

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        // Try the Windows id first, then the IANA id (macOS / Linux).
        foreach (var id in new[] { "Turkey Standard Time", "Europe/Istanbul" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Fallback: fixed UTC+3 with no DST.
        return TimeZoneInfo.CreateCustomTimeZone("TR-Fixed-UTC+3", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}
