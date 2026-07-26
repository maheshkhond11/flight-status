using FlightStatus.Api.Models;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Api.Services;

/// <summary>
/// Normalizes flight status information from various providers into a consistent format.
/// </summary>
public sealed class FlightStatusNormalizer
{
    /// <summary>
    /// Defines the threshold for considering a flight as delayed. If the actual time is later than the scheduled time by this duration, the flight is considered delayed.
    /// </summary>
    private static readonly TimeSpan DelayThreshold = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Normalizes the flight status information from a provider into a consistent format.
    /// </summary>
    /// <param name="providerStatus"></param>
    /// <returns></returns>
    public FlightStatusResult? Normalize(ProviderFlightStatus providerStatus)
    {
        // Attempt to resolve the flight status from the provider's raw status and timing information.
        var status = ResolveStatus(providerStatus);

        if (status is null)
        {
            return null;
        }

        // Construct and return a FlightStatusResult with the normalized information.
        return new FlightStatusResult(
            providerStatus.FlightNumber,
            providerStatus.Date,
            status.Value,
            providerStatus.ScheduledDepartureUtc,
            providerStatus.ActualDepartureUtc,
            providerStatus.ScheduledArrivalUtc,
            providerStatus.ActualArrivalUtc,
            providerStatus.Terminal,
            providerStatus.Gate,
            providerStatus.DelayReason,
            providerStatus.LastUpdatedUtc,
            Message: null);
    }

    private static FlightStatusValue? ResolveStatus(ProviderFlightStatus providerStatus)
    {
        var rawStatus = providerStatus.RawStatus?.Trim().ToUpperInvariant();

        // Check for cancellation status first, as it takes precedence over other statuses.
        if (rawStatus is "CANCELLED" or "CANCELED" or "CXL")
        {
            return FlightStatusValue.Cancelled;
        }

        // Check for diversion or rerouting status next, as it also takes precedence over delay or on-time statuses.
        if (rawStatus is "DIVERTED" or "REROUTED")
        {
            return FlightStatusValue.Diverted;
        }

        // Check for delay status based on scheduled and actual departure/arrival times. If either the departure or arrival is delayed beyond the defined threshold, return Delayed.
        if (IsDelayed(providerStatus.ScheduledDepartureUtc, providerStatus.ActualDepartureUtc) ||
            IsDelayed(providerStatus.ScheduledArrivalUtc, providerStatus.ActualArrivalUtc))
        {
            return FlightStatusValue.Delayed;
        }

        var departureIsComparable = providerStatus.ScheduledDepartureUtc.HasValue && providerStatus.ActualDepartureUtc.HasValue;
        var arrivalIsComparable = providerStatus.ScheduledArrivalUtc.HasValue && providerStatus.ActualArrivalUtc.HasValue;

        // If either the departure or arrival times are comparable (i.e., both scheduled and actual times are available), we can consider the flight as OnTime.
        if (departureIsComparable || arrivalIsComparable)
        {
            return FlightStatusValue.OnTime;
        }

        // If none of the above conditions are met, we fall back to interpreting the raw status string from the provider.
        return rawStatus switch
        {
            "ON_TIME" or "SCHEDULED" => FlightStatusValue.OnTime,
            "DELAYED" or "LATE" => FlightStatusValue.Delayed,
            _ => null
        };
    }

    /// <summary>
    /// Determines if the actual time is delayed compared to the scheduled time based on a predefined threshold.
    /// </summary>
    /// <param name="scheduled"></param>
    /// <param name="actual"></param>
    /// <returns></returns>
    private static bool IsDelayed(DateTimeOffset? scheduled, DateTimeOffset? actual) =>
        scheduled.HasValue && actual.HasValue && actual.Value - scheduled.Value > DelayThreshold;
}
