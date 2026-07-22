using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

public sealed class AeroTrackStubProvider : IFlightStatusProvider
{
    public string Name => "AeroTrack";

    public Task<ProviderFlightStatus?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFlightNumber = flightNumber.Trim().ToUpperInvariant();
        ProviderFlightStatus? result = normalizedFlightNumber switch
        {
            "SR100" => Create(
                date,
                normalizedFlightNumber,
                "ON_TIME",
                scheduledDepartureHour: 10,
                actualDepartureMinute: 10,
                updatedHour: 9,
                updatedMinute: 30,
                terminal: "1",
                gate: "A12",
                delayReason: null),
            "SR200" => Create(
                date,
                normalizedFlightNumber,
                "ON_TIME",
                scheduledDepartureHour: 10,
                actualDepartureMinute: 10,
                updatedHour: 9,
                updatedMinute: 10,
                terminal: "2",
                gate: "B04",
                delayReason: null),
            "SR300" => Create(
                date,
                normalizedFlightNumber,
                "CANCELLED",
                scheduledDepartureHour: 10,
                actualDepartureMinute: null,
                updatedHour: 9,
                updatedMinute: 20,
                terminal: "1",
                gate: "A08",
                delayReason: "Weather disruption"),
            _ => null
        };

        return Task.FromResult(result);
    }

    private static ProviderFlightStatus Create(
        DateOnly date,
        string flightNumber,
        string rawStatus,
        int scheduledDepartureHour,
        int? actualDepartureMinute,
        int updatedHour,
        int updatedMinute,
        string? terminal,
        string? gate,
        string? delayReason)
    {
        var scheduledDeparture = At(date, scheduledDepartureHour, 0);
        DateTimeOffset? actualDeparture = actualDepartureMinute.HasValue
            ? At(date, scheduledDepartureHour, actualDepartureMinute.Value)
            : null;

        return new ProviderFlightStatus(
            ProviderName: "AeroTrack",
            FlightNumber: flightNumber,
            Date: date,
            RawStatus: rawStatus,
            ScheduledDepartureUtc: scheduledDeparture,
            ActualDepartureUtc: actualDeparture,
            ScheduledArrivalUtc: At(date, 12, 0),
            ActualArrivalUtc: actualDepartureMinute.HasValue ? At(date, 12, actualDepartureMinute.Value) : (DateTimeOffset?)null,
            Terminal: terminal,
            Gate: gate,
            DelayReason: delayReason,
            LastUpdatedUtc: At(date, updatedHour, updatedMinute));
    }

    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, TimeSpan.Zero);
}
