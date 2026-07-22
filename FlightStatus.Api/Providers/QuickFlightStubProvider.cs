using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

public sealed class QuickFlightStubProvider : IFlightStatusProvider
{
    public string Name => "QuickFlight";

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

    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, TimeSpan.Zero);
}
