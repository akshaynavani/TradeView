using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TradeView";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? "Data Source=tradeview.db";

        services.AddDbContext<TradeViewDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
