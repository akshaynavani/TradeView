using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeView.Core.Abstractions;
using TradeView.Infrastructure.MarketData;
using TradeView.Infrastructure.Persistence;
using TradeView.Infrastructure.Repositories;

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

        services.Configure<MarketDataOptions>(configuration.GetSection(MarketDataOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMarketDataFeed, SimulatedMarketDataFeed>();

        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<IPriceTickRepository, PriceTickRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();

        return services;
    }
}
