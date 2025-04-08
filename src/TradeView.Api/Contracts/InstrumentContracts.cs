using System.ComponentModel.DataAnnotations;
using TradeView.Core.Domain;

namespace TradeView.Api.Contracts;

public sealed record InstrumentResponse(
    int Id,
    string Symbol,
    string Name,
    AssetClass AssetClass,
    string Currency,
    bool IsActive,
    DateTime CreatedAtUtc)
{
    public static InstrumentResponse From(Instrument i) =>
        new(i.Id, i.Symbol, i.Name, i.AssetClass, i.Currency, i.IsActive, i.CreatedAtUtc);
}

public sealed class CreateInstrumentRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9.\\-]+$", ErrorMessage = "Symbol may contain letters, digits, '.' and '-'.")]
    public string Symbol { get; init; } = string.Empty;

    [StringLength(200)]
    public string? Name { get; init; }

    public AssetClass AssetClass { get; init; } = AssetClass.Equity;

    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";

    /// <summary>Starting price for the simulated feed.</summary>
    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal SeedPrice { get; init; }
}

public sealed class UpdateInstrumentRequest
{
    [StringLength(200)]
    public string? Name { get; init; }

    public bool? IsActive { get; init; }
}
