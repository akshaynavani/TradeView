using Microsoft.AspNetCore.Http;
using TradeView.Api.Infrastructure;
using TradeView.Core.Exceptions;

namespace TradeView.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int> Cases => new()
    {
        { new NotFoundException("Instrument", "XYZ"), StatusCodes.Status404NotFound },
        { new DomainValidationException("bad"), StatusCodes.Status400BadRequest },
        { new ConflictException("dupe"), StatusCodes.Status409Conflict },
        { new InvalidOperationException("boom"), StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Maps_exceptions_to_http_status(Exception exception, int expectedStatus)
    {
        var (status, _) = GlobalExceptionHandler.Map(exception);

        Assert.Equal(expectedStatus, status);
    }
}
