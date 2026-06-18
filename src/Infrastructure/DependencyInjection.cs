using Microsoft.Extensions.DependencyInjection;
using HotelPOS.Application.Interfaces;
using HotelPOS.Infrastructure.Persistence;

namespace HotelPOS.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure repository implementations in the dependency injection container.
    /// </summary>
    /// <returns>The same <see cref="IServiceCollection"/> instance with the infrastructure repositories registered.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<ICashRepository, CashRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHeldOrderRepository, HeldOrderRepository>();

        return services;
    }
}
