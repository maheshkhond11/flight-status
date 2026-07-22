using FlightStatus.Api.Models;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Api.Services;

public sealed class FlightStatusNormalizer
{
    private static readonly TimeSpan DelayThreshold = TimeSpan.FromMinutes(15);

    public FlightStatusResult? Normalize(ProviderFlightStatus providerStatus)
    {
        var status = ResolveStatus(providerStatus);
        if (status is null)
        {
            return null;
        }

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

        if (rawStatus is "CANCELLED" or "CANCELED" or "CXL")
        {
            return FlightStatusValue.Cancelled;
        }

        if (rawStatus is "DIVERTED" or "REROUTED")
        {
            return FlightStatusValue.Diverted;
        }

        var departureIsComparable = providerStatus.ScheduledDepartureUtc.HasValue && providerStatus.ActualDepartureUtc.HasValue;
        var arrivalIsComparable = providerStatus.ScheduledArrivalUtc.HasValue && providerStatus.ActualArrivalUtc.HasValue;

        if (IsDelayed(providerStatus.ScheduledDepartureUtc, providerStatus.ActualDepartureUtc) ||
            IsDelayed(providerStatus.ScheduledArrivalUtc, providerStatus.ActualArrivalUtc))
        {
            return FlightStatusValue.Delayed;
        }

        if (departureIsComparable || arrivalIsComparable)
        {
            return FlightStatusValue.OnTime;
        }

        return rawStatus switch
        {
            "ON_TIME" or "SCHEDULED" => FlightStatusValue.OnTime,
            "DELAYED" or "LATE" => FlightStatusValue.Delayed,
            _ => null
        };
    }

    private static bool IsDelayed(DateTimeOffset? scheduled, DateTimeOffset? actual) =>
        scheduled.HasValue && actual.HasValue && actual.Value - scheduled.Value > DelayThreshold;
}
