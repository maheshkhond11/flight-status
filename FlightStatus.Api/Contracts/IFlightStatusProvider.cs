using FlightStatus.Api.Models;

namespace FlightStatus.Api.Contracts;

/// <summary>
/// Defines a contract for flight status providers that can retrieve flight status information based on flight number and date.
/// </summary>
public interface IFlightStatusProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the flight status for the given flight number and date.
    /// </summary>
    /// <param name="flightNumber"></param>
    /// <param name="date"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken);
}
