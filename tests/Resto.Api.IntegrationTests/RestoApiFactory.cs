using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Resto.Api.IntegrationTests;

public sealed class RestoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public RestoApiFactory()
    {
        var databaseName = $"RestoIntegrationTest_{Guid.NewGuid():N}";

        var ciConnection = Environment.GetEnvironmentVariable("RESTO_TEST_CONNECTION");
        _connectionString = string.IsNullOrWhiteSpace(ciConnection)
            ? $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True"
            : ReplaceDatabaseName(ciConnection, databaseName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
    }

    private static string ReplaceDatabaseName(string connectionString, string databaseName)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var updated = parts
            .Select(part =>
                part.TrimStart().StartsWith("Database=", StringComparison.OrdinalIgnoreCase)
                    ? $"Database={databaseName}"
                    : part)
            .ToList();

        if (updated.All(part => !part.TrimStart().StartsWith("Database=", StringComparison.OrdinalIgnoreCase)))
            updated.Add($"Database={databaseName}");

        return string.Join(';', updated);
    }
}
