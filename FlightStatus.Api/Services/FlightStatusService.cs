using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Api.Services;

public sealed class FlightStatusService
{
    private const string UnknownStatusMessage = "No usable status was returned by either provider.";
    private readonly IReadOnlyCollection<IFlightStatusProvider> _providers;
    private readonly FlightStatusNormalizer _normalizer;
    private readonly ILogger<FlightStatusService> _logger;

    public FlightStatusService(
        IEnumerable<IFlightStatusProvider> providers,
        FlightStatusNormalizer normalizer,
        ILogger<FlightStatusService> logger)
    {
        _providers = providers.ToArray();
        _normalizer = normalizer;
        _logger = logger;
    }

    public async Task<FlightStatusResult> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var candidates = await Task.WhenAll(_providers.Select(provider =>
            GetUsableResponseAsync(provider, flightNumber, date, cancellationToken)));

        var winner = candidates
            .OfType<ProviderCandidate>()
            .OrderByDescending(candidate => candidate.Result.LastUpdatedUtc)
            .ThenBy(candidate => candidate.ProviderName.Equals("AeroTrack", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(candidate => candidate.Result)
            .FirstOrDefault();

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
            var rawResponse = await provider.GetStatusAsync(flightNumber, date, cancellationToken);
            var normalizedResponse = rawResponse is null ? null : _normalizer.Normalize(rawResponse);
            return normalizedResponse is null ? null : new ProviderCandidate(provider.Name, normalizedResponse);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Flight-status provider {ProviderName} did not respond", provider.Name);
            return null;
        }
    }

    private static FlightStatusResult CreateUnknownResult(string flightNumber, DateOnly date) =>
        new(
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

    private sealed record ProviderCandidate(string ProviderName, FlightStatusResult Result);
}
