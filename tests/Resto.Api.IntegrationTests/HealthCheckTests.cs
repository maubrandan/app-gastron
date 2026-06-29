namespace Resto.Api.IntegrationTests;

using FluentAssertions;

public sealed class HealthCheckTests : IClassFixture<RestoApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(RestoApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Health_IncludesCorrelationIdHeader()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.GetValues("X-Correlation-Id").Single().Should().Be(correlationId);
    }
}
