using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TradeView.Core.Exceptions;

namespace TradeView.Api.Infrastructure;

/// <summary>Translates exceptions into RFC 7807 problem responses.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Only expected domain errors expose their message to clients.
                Detail = exception is TradeViewException ? exception.Message : null,
                Instance = httpContext.Request.Path
            }
        });
    }

    internal static (int Status, string Title) Map(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        DomainValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
        OperationCanceledException => (499, "Client closed request"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };
}
