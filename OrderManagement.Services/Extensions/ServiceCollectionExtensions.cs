using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Model.Interfaces;

namespace OrderManagement.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
