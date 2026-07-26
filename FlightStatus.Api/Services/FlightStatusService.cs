using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

// Alias avoids a naming clash between the namespace and the FlightStatus enum.
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Api.Services;

public sealed class FlightStatusService
{
    /// <summary>
    /// A message to include in the flight status result when no provider returned a usable status.
    /// </summary>
    private const string UnknownStatusMessage = "No usable status was returned by either provider.";

    /// <summary>
    /// Gets the collection of flight status providers that can be queried for flight status information.
    /// </summary>
    private readonly IReadOnlyCollection<IFlightStatusProvider> _providers;

    /// <summary>
    /// Gets the normalizer for converting provider-specific flight status information into a consistent format.
    /// </summary>
    private readonly FlightStatusNormalizer _normalizer;

    /// <summary>
    /// Gets the logger for the flight status service.
    /// </summary>
    private readonly ILogger<FlightStatusService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightStatusService"/> class with the specified providers, normalizer, and logger.
    /// </summary>
    /// <param name="providers"></param>
    /// <param name="normalizer"></param>
    /// <param name="logger"></param>
    public FlightStatusService(
        IEnumerable<IFlightStatusProvider> providers,
        FlightStatusNormalizer normalizer,
        ILogger<FlightStatusService> logger)
    {
        // Materialize the providers once so the collection can be reused safely.
        _providers = providers.ToArray();
        _normalizer = normalizer;
        _logger = logger;
    }

    /// <summary>
    /// Gets the flight status for a given flight number and date, using multiple providers to find the most recent and usable result.
    /// </summary>
    /// <param name="flightNumber"></param>
    /// <param name="date"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<FlightStatusResult> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        // Ask every provider at the same time instead of waiting for each in sequence.
        var candidates = await Task.WhenAll(
            _providers.Select(provider =>
                GetUsableResponseAsync(provider, flightNumber, date, cancellationToken)));

        // Keep valid responses, choose the newest one, and prefer AeroTrack on a tie.
        var winner = candidates
            .OfType<ProviderCandidate>()
            .OrderByDescending(candidate => candidate.Result.LastUpdatedUtc)
            .ThenBy(candidate =>
                candidate.ProviderName.Equals(
                    "AeroTrack",
                    StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .Select(candidate => candidate.Result)
            .FirstOrDefault();

        // Return a safe fallback when no provider supplied a usable status.
        return winner ?? CreateUnknownResult(flightNumber, date);
    }

    /// <summary>
    /// Attempts to get a usable flight status from a single provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="flightNumber"></param>
    /// <param name="date"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ProviderCandidate?> GetUsableResponseAsync(
        IFlightStatusProvider provider,
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            // Fetch the provider-specific response.
            var rawResponse = await provider.GetStatusAsync(
                flightNumber,
                date,
                cancellationToken);

            // Convert it to the application's standard result format.
            var normalizedResponse = rawResponse is null
                ? null
                : _normalizer.Normalize(rawResponse);

            // Ignore null or unusable provider responses.
            return normalizedResponse is null
                ? null
                : new ProviderCandidate(provider.Name, normalizedResponse);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A provider failure should not prevent other providers from succeeding.
            // Cancellation is deliberately not caught, so it propagates to the caller.
            _logger.LogWarning(
                exception,
                "Flight-status provider {ProviderName} did not respond",
                provider.Name);

            return null;
        }
    }

    /// <summary>
    /// Creates a fallback result when no provider returned a usable status.
    /// </summary>
    /// <param name="flightNumber"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    private static FlightStatusResult CreateUnknownResult(
        string flightNumber,
        DateOnly date) =>
        new(
            // Return a consistently formatted flight number in the fallback result.
            flightNumber.Trim().ToUpperInvariant(),
            date,
            FlightStatusValue.Unknown,
            ScheduledDepartureUtc: null,
            ActualDepartureUtc: null,
            ScheduledArrivalUtc: null,
            ActualArrivalUtc: null,
            Terminal: null,
            Gate: null,
            DelayReason: null,
            LastUpdatedUtc: null,
            Message: UnknownStatusMessage);

    /// <summary>
    /// Represents a candidate response from a provider that returned a usable flight status.
    /// </summary>
    /// <param name="ProviderName"></param>
    /// <param name="Result"></param>
    private sealed record ProviderCandidate(string ProviderName, FlightStatusResult Result);
}