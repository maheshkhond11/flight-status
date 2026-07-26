using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// A stub implementation of the IFlightStatusProvider interface for testing purposes.
/// </summary>
public sealed class QuickFlightStubProvider : IFlightStatusProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => "QuickFlight";

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
            "SR100" => Create(date, normalizedFlightNumber, "LATE", 9, 10),
            "SR200" => Create(date, normalizedFlightNumber, "LATE", 9, 35),
            "SR400" => Create(date, normalizedFlightNumber, "REROUTED", 9, 20),
            _ => null
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Creates a ProviderFlightStatus instance with the specified parameters.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="flightNumber"></param>
    /// <param name="rawStatus"></param>
    /// <param name="updatedHour"></param>
    /// <param name="updatedMinute"></param>
    /// <returns></returns>
    private static ProviderFlightStatus Create(
        DateOnly date,
        string flightNumber,
        string rawStatus,
        int updatedHour,
        int updatedMinute) =>
        new(
            ProviderName: "QuickFlight",
            FlightNumber: flightNumber,
            Date: date,
            RawStatus: rawStatus,
            ScheduledDepartureUtc: At(date, 10, 0),
            ActualDepartureUtc: null,
            ScheduledArrivalUtc: At(date, 12, 0),
            ActualArrivalUtc: null,
            Terminal: null,
            Gate: null,
            DelayReason: null,
            LastUpdatedUtc: At(date, updatedHour, updatedMinute));

    /// <summary>
    /// Creates a DateTimeOffset instance for the specified date, hour, and minute in UTC.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="hour"></param>
    /// <param name="minute"></param>
    /// <returns></returns>
    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, TimeSpan.Zero);
}
