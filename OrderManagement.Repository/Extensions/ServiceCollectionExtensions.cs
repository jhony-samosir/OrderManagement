using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Model.Interfaces;
using OrderManagement.Repository.Repositories;

namespace OrderManagement.Repository.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(connectionString, npgsql =>
            {

                npgsql.CommandTimeout(30);
            }));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }

    public static async Task MigrateAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
