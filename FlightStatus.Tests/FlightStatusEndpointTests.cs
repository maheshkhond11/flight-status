using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightStatus.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using FlightStatusValue = FlightStatus.Api.Models.FlightStatus;

namespace FlightStatus.Tests;

public class FlightStatusEndpointTests(TestApplicationFactory factory) : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetStatus_MissingFlightNumber_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/flights/status?date=2026-07-22");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_InvalidDate_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=SR100&date=22-07-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_SR100_ReturnsAeroTrackOnTimeResult()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=sr100&date=2026-07-22");
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.OnTime, result.Status);
        Assert.Equal("A12", result.Gate);
    }

    [Fact]
    public async Task GetStatus_SR999_ReturnsUnknownWithMessage()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=SR999&date=2026-07-22");
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Unknown, result.Status);
        Assert.Equal("No usable status was returned by either provider.", result.Message);
    }

    /// <summary>
    /// Regression guard for spec.md section 8: AeroTrack's own SR200 timing data normalises to
    /// OnTime (its 10-minute actual delay sits inside the 15-minute threshold). QuickFlight must
    /// still win and report Delayed, purely because its lastUpdatedUtc is later -- if a future
    /// change to the normalizer or merge logic ever made AeroTrack look Delayed too, or made the
    /// merge stop respecting recency, this test would catch it.
    /// </summary>
    [Fact]
    public async Task GetStatus_SR200_QuickFlightWinsOnRecencyAndReportsDelayed()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=SR200&date=2026-07-22");
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Delayed, result.Status);
        // QuickFlight never supplies terminal/gate, so a non-null value here would mean AeroTrack's
        // (OnTime) response was mistakenly merged in.
        Assert.Null(result.Terminal);
        Assert.Null(result.Gate);
    }

    [Fact]
    public async Task GetStatus_SR300_ReturnsCancelledFromAeroTrackOnly()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=SR300&date=2026-07-22");
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Cancelled, result.Status);
        Assert.Equal("Weather disruption", result.DelayReason);
    }

    [Fact]
    public async Task GetStatus_SR400_ReturnsDivertedFromQuickFlightOnly()
    {
        var response = await _client.GetAsync("/flights/status?flightNumber=SR400&date=2026-07-22");
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(FlightStatusValue.Diverted, result.Status);
    }
}

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
