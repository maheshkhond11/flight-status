using FlightStatus.Api.Contracts;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Tests;

public class FlightStatusServiceTests
{
    private static readonly DateOnly Date = new(2026, 7, 22);

    [Fact]
    public async Task GetStatusAsync_BothProvidersReturnUsableData_UsesMostRecentlyUpdatedResponse()
    {
        var aeroTrack = new StubProvider("AeroTrack", CreateResponse("AeroTrack", "ON_TIME", 9, 10, actualMinute: 10));
        var quickFlight = new StubProvider("QuickFlight", CreateResponse("QuickFlight", "LATE", 9, 35, actualMinute: null));
        var service = CreateService(aeroTrack, quickFlight);

        var result = await service.GetStatusAsync("SR200", Date, CancellationToken.None);

        Assert.Equal(FlightStatusValue.Delayed, result.Status);
        Assert.Equal(At(9, 35), result.LastUpdatedUtc);
    }

    [Fact]
    public async Task GetStatusAsync_ProviderFailsButOtherProviderSucceeds_ReturnsSuccessfulProviderResponse()
    {
        var failedProvider = new StubProvider("AeroTrack", exception: new HttpRequestException("Unavailable"));
        var successfulProvider = new StubProvider("QuickFlight", CreateResponse("QuickFlight", "SCHEDULED", 9, 20, actualMinute: null));
        var service = CreateService(failedProvider, successfulProvider);

        var result = await service.GetStatusAsync("SR100", Date, CancellationToken.None);

        Assert.Equal(FlightStatusValue.OnTime, result.Status);
        Assert.Equal(At(9, 20), result.LastUpdatedUtc);
    }

    [Fact]
    public async Task GetStatusAsync_NeitherProviderHasAResult_ReturnsUnknownWithClearMessage()
    {
        var service = CreateService(
            new StubProvider("AeroTrack"),
            new StubProvider("QuickFlight"));

        var result = await service.GetStatusAsync("SR999", Date, CancellationToken.None);

        Assert.Equal(FlightStatusValue.Unknown, result.Status);
        Assert.Equal("No usable status was returned by either provider.", result.Message);
    }

    [Fact]
    public async Task GetStatusAsync_EqualUpdateTimes_PrefersAeroTrack()
    {
        var aeroTrack = new StubProvider("AeroTrack", CreateResponse("AeroTrack", "CANCELLED", 9, 20, actualMinute: null));
        var quickFlight = new StubProvider("QuickFlight", CreateResponse("QuickFlight", "REROUTED", 9, 20, actualMinute: null));
        var service = CreateService(aeroTrack, quickFlight);

        var result = await service.GetStatusAsync("SR300", Date, CancellationToken.None);

        Assert.Equal(FlightStatusValue.Cancelled, result.Status);
    }

    private static FlightStatusService CreateService(params IFlightStatusProvider[] providers) =>
        new(providers, new FlightStatusNormalizer(), NullLogger<FlightStatusService>.Instance);

    private static ProviderFlightStatus CreateResponse(
        string providerName,
        string rawStatus,
        int updatedHour,
        int updatedMinute,
        int? actualMinute) =>
        new(
            providerName,
            "SR100",
            Date,
            rawStatus,
            ScheduledDepartureUtc: At(10, 0),
            ActualDepartureUtc: actualMinute.HasValue ? At(10, actualMinute.Value) : null,
            ScheduledArrivalUtc: null,
            ActualArrivalUtc: null,
            Terminal: providerName == "AeroTrack" ? "1" : null,
            Gate: providerName == "AeroTrack" ? "A12" : null,
            DelayReason: null,
            LastUpdatedUtc: At(updatedHour, updatedMinute));

    private static DateTimeOffset At(int hour, int minute) =>
        new(Date.Year, Date.Month, Date.Day, hour, minute, 0, TimeSpan.Zero);

    private sealed class StubProvider : IFlightStatusProvider
    {
        private readonly ProviderFlightStatus? _response;
        private readonly Exception? _exception;

        public StubProvider(string name, ProviderFlightStatus? response = null, Exception? exception = null)
        {
            Name = name;
            _response = response;
            _exception = exception;
        }

        public string Name { get; }

        public Task<ProviderFlightStatus?> GetStatusAsync(
            string flightNumber,
            DateOnly date,
            CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_response);
        }
    }
}
