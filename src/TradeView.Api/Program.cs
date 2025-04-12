using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using TradeView.Api.BackgroundServices;
using TradeView.Api.Extensions;
using TradeView.Api.Infrastructure;
using TradeView.Core.Abstractions;
using TradeView.Infrastructure;
using TradeView.Infrastructure.MarketData;
using TradeView.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTradeViewServices();
builder.Services.AddHostedService<MarketDataIngestionService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<TradeViewDbContext>("database");

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TradeView Market Data API",
        Version = "v1",
        Description = "Real-time market data, trade P&L and configurable price alerts."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TradeViewDbContext>();
    await db.Database.EnsureCreatedAsync();

    var marketDataOptions = scope.ServiceProvider.GetRequiredService<IOptions<MarketDataOptions>>().Value;
    if (marketDataOptions.SeedInstruments)
    {
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IClock>());
    }
}

public partial class Program;
