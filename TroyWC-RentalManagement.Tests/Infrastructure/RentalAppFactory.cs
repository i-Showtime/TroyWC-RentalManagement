using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TroyWC_RentalManagement.Data;

namespace TroyWC_RentalManagement.Tests.Infrastructure;

/// <summary>
/// Hosts the real app in memory against a LocalDB database created for this test run and dropped afterwards.
/// Shared by every test through <see cref="AppCollection"/>; tests stay independent by creating their own
/// users and units.
/// </summary>
public sealed class RentalAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string DatabaseName { get; } = $"RentalMgmt_Tests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $@"Server=(localdb)\mssqllocaldb;Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development, so no Bogus seed data; the app still migrates its (test) database at startup.
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

        // Program builds its DbContext options from the connection string it read at startup;
        // this runs after that registration, so the test database wins however configuration is ordered.
        builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<ApplicationDbContext>(options => options.UseSqlServer(ConnectionString)));
    }

    public Task InitializeAsync()
    {
        // Starting the host migrates the database and creates the roles.
        using var scope = Services.CreateScope();

        // Never let a configuration mistake point the tests at a real database.
        var actual = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.GetConnectionString();
        if (actual?.Contains(DatabaseName, StringComparison.Ordinal) != true)
            throw new InvalidOperationException($"The app is not using the test database: {actual}");

        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// A context on the test database, for arranging and checking data directly. It uses the app's services so its
    /// model matches the app's (Identity's table options come from there).
    /// </summary>
    public ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .UseApplicationServiceProvider(Services)
            .Options);

    /// <summary>A browser-like client with its own cookies. Redirects are not followed so tests can check them.</summary>
    public AppClient CreateAppClient() =>
        new(CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        }));
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<RentalAppFactory>
{
    public const string Name = "App";
}
