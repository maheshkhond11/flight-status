using FlightStatus.Api.Models;
using FlightStatus.Api.Services;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Tests;

public class FlightStatusNormalizerTests
{
    private readonly FlightStatusNormalizer _normalizer = new();
    private static readonly DateOnly Date = new(2026, 7, 22);

    [Fact]
    public void Normalize_ActualDepartureExactlyFifteenMinutesLate_ReturnsOnTime()
    {
        var result = _normalizer.Normalize(CreateStatus("ON_TIME", actualDepartureMinute: 15));

        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.OnTime, result.Status);
    }

    [Fact]
    public void Normalize_ActualDepartureSixteenMinutesLate_ReturnsDelayed()
    {
        var result = _normalizer.Normalize(CreateStatus("ON_TIME", actualDepartureMinute: 16));

        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Delayed, result.Status);
    }

    [Fact]
    public void Normalize_CancelledStatusOverridesLateTiming()
    {
        var result = _normalizer.Normalize(CreateStatus("CXL", actualDepartureMinute: 45));

        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Cancelled, result.Status);
    }

    [Theory]
    [InlineData("LATE", FlightStatusValue.Delayed)]
    [InlineData("SCHEDULED", FlightStatusValue.OnTime)]
    [InlineData("REROUTED", FlightStatusValue.Diverted)]
    public void Normalize_KnownProviderVocabulary_ReturnsUnifiedStatus(string rawStatus, FlightStatusValue expectedStatus)
    {
        var result = _normalizer.Normalize(CreateStatus(rawStatus, actualDepartureMinute: null));

        Assert.NotNull(result);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public void Normalize_UnknownStatusWithoutComparableTimes_ReturnsNull()
    {
        var result = _normalizer.Normalize(CreateStatus("PENDING", actualDepartureMinute: null));

        Assert.Null(result);
    }

    private static ProviderFlightStatus CreateStatus(string rawStatus, int? actualDepartureMinute)
    {
        var scheduledDeparture = At(10, 0);
        return new ProviderFlightStatus(
            "TestProvider",
            "SR100",
            Date,
            rawStatus,
            scheduledDeparture,
            actualDepartureMinute.HasValue ? At(10, actualDepartureMinute.Value) : null,
            ScheduledArrivalUtc: null,
            ActualArrivalUtc: null,
            Terminal: null,
            Gate: null,
            DelayReason: null,
            LastUpdatedUtc: At(9, 0));
    }

    private static DateTimeOffset At(int hour, int minute) =>
        new(Date.Year, Date.Month, Date.Day, hour, minute, 0, TimeSpan.Zero);
}
