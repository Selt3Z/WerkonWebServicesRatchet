namespace WerkonWebServicesRatchet.Infrastructure.Time;

public sealed class AppTimeZone
{
    private TimeZoneInfo _timeZone;
    private readonly ILogger<AppTimeZone> _logger;

    public AppTimeZone(IConfiguration configuration, ILogger<AppTimeZone> logger)
    {
        _logger = logger;
        _timeZone = ResolveTimeZone(configuration["AppTimeZone"], logger);
    }

    public string TimeZoneId => _timeZone.Id;

    public TimeZoneInfo TimeZoneInfo => _timeZone;

    public void SetTimeZoneId(string timeZoneId)
    {
        _timeZone = ResolveTimeZone(timeZoneId, _logger);
    }

    public DateOnly GetToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone));

    public (DateTime StartUtc, DateTime EndUtc) GetDayRangeUtc(DateOnly date)
    {
        var startLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone));
    }

    public DateTime ToUtcStartOfDay(DateOnly date) =>
        GetDayRangeUtc(date).StartUtc;

    public DateTime FromUtc(DateTime utcDateTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), _timeZone);

    public DateTime ToUtc(DateTime localDateTime) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), _timeZone);

    private static TimeZoneInfo ResolveTimeZone(string? configuredId, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(configuredId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(configuredId, out var configuredTimeZone))
        {
            logger.LogInformation("Using AppTimeZone: {TimeZoneId}", configuredTimeZone.Id);
            return configuredTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(configuredId))
        {
            logger.LogWarning(
                "AppTimeZone value \"{ConfiguredId}\" was not found. Falling back to local server timezone: {LocalId}",
                configuredId,
                TimeZoneInfo.Local.Id);
        }

        return TimeZoneInfo.Local;
    }
}
