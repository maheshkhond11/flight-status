using FlightStatus.Api.Models;

namespace FlightStatus.Api.Contracts;

public interface IFlightStatusProvider
{
    string Name { get; }

    Task<ProviderFlightStatus?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken);
}
