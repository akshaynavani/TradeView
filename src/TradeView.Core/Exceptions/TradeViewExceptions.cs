namespace TradeView.Core.Exceptions;

/// <summary>Base type for expected, client-facing errors.</summary>
public abstract class TradeViewException : Exception
{
    protected TradeViewException(string message)
        : base(message)
    {
    }
}

/// <summary>A referenced resource (instrument, alert, trade) does not exist.</summary>
public sealed class NotFoundException : TradeViewException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
    }

    public string Resource { get; }

    public object Key { get; }
}

/// <summary>A request violates a business rule (e.g. selling more than is held).</summary>
public sealed class DomainValidationException : TradeViewException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}

/// <summary>A request conflicts with existing state (e.g. duplicate symbol).</summary>
public sealed class ConflictException : TradeViewException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
