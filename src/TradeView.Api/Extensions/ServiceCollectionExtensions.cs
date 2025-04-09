using TradeView.Core.Services;

namespace TradeView.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the Core application services.</summary>
    public static IServiceCollection AddTradeViewServices(this IServiceCollection services)
    {
        services.AddScoped<InstrumentService>();
        services.AddScoped<MarketDataService>();
        services.AddScoped<PortfolioService>();
        return services;
    }
}
