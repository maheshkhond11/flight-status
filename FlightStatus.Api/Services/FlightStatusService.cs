using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;

// Alias avoids a naming clash between the namespace and the FlightStatus enum.
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Api.Services;

public sealed class FlightStatusService
{
    // Returned when every provider fails or returns an unusable response.
    private const string UnknownStatusMessage =
        "No usable status was returned by either provider.";

    private readonly IReadOnlyCollection<IFlightStatusProvider> _providers;
    private readonly FlightStatusNormalizer _normalizer;
    private readonly ILogger<FlightStatusService> _logger;

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

    // Keeps a normalized result together with the name of its source provider.
    private sealed record ProviderCandidate(string ProviderName, FlightStatusResult Result);
}