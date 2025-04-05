using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Exceptions;

namespace TradeView.Core.Services;

public sealed class InstrumentService
{
    private readonly IInstrumentRepository _instruments;
    private readonly IClock _clock;

    public InstrumentService(IInstrumentRepository instruments, IClock clock)
    {
        _instruments = instruments;
        _clock = clock;
    }

    public static string NormalizeSymbol(string symbol) => symbol.Trim().ToUpperInvariant();

    public Task<IReadOnlyList<Instrument>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
        => _instruments.GetAllAsync(activeOnly, cancellationToken);

    public async Task<Instrument> GetRequiredAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        return await _instruments.GetBySymbolAsync(normalized, cancellationToken)
            ?? throw new NotFoundException("Instrument", normalized);
    }

    public async Task<Instrument> CreateAsync(
        string symbol,
        string name,
        AssetClass assetClass,
        string currency,
        decimal seedPrice,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainValidationException("Symbol is required.");
        }

        if (seedPrice <= 0)
        {
            throw new DomainValidationException("Seed price must be greater than zero.");
        }

        if (await _instruments.SymbolExistsAsync(normalized, cancellationToken))
        {
            throw new ConflictException($"Instrument '{normalized}' already exists.");
        }

        var instrument = new Instrument
        {
            Symbol = normalized,
            Name = string.IsNullOrWhiteSpace(name) ? normalized : name.Trim(),
            AssetClass = assetClass,
            Currency = currency.Trim().ToUpperInvariant(),
            SeedPrice = seedPrice,
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        await _instruments.AddAsync(instrument, cancellationToken);
        await _instruments.SaveChangesAsync(cancellationToken);
        return instrument;
    }

    public async Task<Instrument> UpdateAsync(
        string symbol,
        string? name,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetRequiredAsync(symbol, cancellationToken);

        if (!string.IsNullOrWhiteSpace(name))
        {
            instrument.Name = name.Trim();
        }

        if (isActive.HasValue)
        {
            instrument.IsActive = isActive.Value;
        }

        await _instruments.SaveChangesAsync(cancellationToken);
        return instrument;
    }
}
