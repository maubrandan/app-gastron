using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Resto.Api.IntegrationTests;

internal sealed class RestoApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _client;

    public RestoApiClient(HttpClient client) => _client = client;

    public async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        login.Should().NotBeNull();
        login!.Token.Should().NotBeNullOrWhiteSpace();

        return login.Token;
    }

    public void SetBearerToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return body ?? throw new InvalidOperationException($"Respuesta vacía de GET {url}");
    }

    public async Task<T> PostAsync<T>(string url, object payload)
    {
        var response = await _client.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return body ?? throw new InvalidOperationException($"Respuesta vacía de POST {url}");
    }

    public async Task<HttpResponseMessage> PostRawAsync(string url, object payload) =>
        await _client.PostAsJsonAsync(url, payload);

    internal sealed record LoginResponse(string Token, DateTime ExpiresAt, LoginUserResponse User);

    internal sealed record LoginUserResponse(Guid Id, string Email, string DisplayName, string[] Roles);

    internal sealed record TableResponse(int Number, string Status, string RowVersion, Guid? ActiveOrderId);

    internal sealed record CreateOrderResponse(Guid OrderId, string OrderRowVersion, string TableRowVersion);

    internal sealed record RowVersionResponse(string RowVersion);

    internal sealed record ProductResponse(Guid Id, string Name, decimal Price, string Category, bool IsActive);

    internal sealed record KitchenOrderResponse(Guid Id, int TableNumber, DateTime SentToKitchenAt);

    internal sealed record OrderResponse(
        Guid Id,
        int TableNumber,
        string Status,
        decimal Total,
        string RowVersion,
        DateTime? ClosedAt);

    internal sealed record DailySummaryResponse(
        int OrderCount,
        decimal TotalRevenue,
        decimal AverageTicket);

    internal sealed record ProblemResponse(string? Type, string? Title, int Status, string? Detail);
}
