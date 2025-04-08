using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TradeView.Api.BackgroundServices;
using TradeView.Api.Extensions;
using TradeView.Core.Abstractions;
using TradeView.Infrastructure;
using TradeView.Infrastructure.MarketData;
using TradeView.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTradeViewServices();
builder.Services.AddHostedService<MarketDataIngestionService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TradeViewDbContext>();
    await db.Database.EnsureCreatedAsync();

    var marketDataOptions = scope.ServiceProvider.GetRequiredService<IOptions<MarketDataOptions>>().Value;
    if (marketDataOptions.SeedInstruments)
    {
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IClock>());
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
