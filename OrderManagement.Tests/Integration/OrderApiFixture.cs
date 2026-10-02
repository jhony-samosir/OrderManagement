using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Model.Entities;
using OrderManagement.Repository;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderManagement.Tests.Integration;

public sealed class OrderApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("order_test_db")
        .WithUsername("test")
        .WithPassword("test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
    public async Task SeedProductAsync(Product product)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });

        builder.UseEnvironment("Testing");
    }
}
