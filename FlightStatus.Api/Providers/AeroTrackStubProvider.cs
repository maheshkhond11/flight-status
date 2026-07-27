using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// A stub implementation of the AeroTrack flight status provider for testing purposes.
/// </summary>
public sealed class AeroTrackStubProvider : IFlightStatusProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => "AeroTrack";

    /// <summary>
    /// Gets the flight status for a given flight number and date.
    /// </summary>
    /// <param name="flightNumber"></param>
    /// <param name="date"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
            // AeroTrack genuinely is on-time here (10-minute actual delay stays inside the
            // 15-minute threshold, see FlightStatusNormalizer). QuickFlight still wins this
            // scenario per spec.md section 8 because its lastUpdatedUtc (09:35Z) is later
            // than AeroTrack's (09:10Z) -- this is a timestamp-recency win, not a status
            // conflict, so do not "fix" this data to make AeroTrack look delayed.
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

        // Return the result as a completed task
        return Task.FromResult(result);
    }

    /// <summary>
    /// Creates a ProviderFlightStatus instance with the provided parameters.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="flightNumber"></param>
    /// <param name="rawStatus"></param>
    /// <param name="scheduledDepartureHour"></param>
    /// <param name="actualDepartureMinute"></param>
    /// <param name="updatedHour"></param>
    /// <param name="updatedMinute"></param>
    /// <param name="terminal"></param>
    /// <param name="gate"></param>
    /// <param name="delayReason"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a DateTimeOffset for the specified date, hour, and minute in UTC.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="hour"></param>
    /// <param name="minute"></param>
    /// <returns></returns>
    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, TimeSpan.Zero);
}
