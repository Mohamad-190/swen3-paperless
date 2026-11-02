using System.Text.Json;
using DocVault.Core.Exceptions;
using DocVault.WebApi.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocVault.Tests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    public static TheoryData<Exception, int> StatusCodeCases => new()
    {
        { new DocumentNotFoundException(Guid.NewGuid()), StatusCodes.Status404NotFound },
        { new InvalidFolderException("Name must not be empty."), StatusCodes.Status400BadRequest },
        { new MessagingException("broker down", new IOException()), StatusCodes.Status503ServiceUnavailable },
        { new DataAccessException("db down", new InvalidOperationException()), StatusCodes.Status500InternalServerError },
        { new InvalidOperationException("boom"), StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(StatusCodeCases))]
    public async Task TryHandleAsync_MapsExceptionToStatusCode(Exception exception, int expectedStatus)
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_BusinessError_ReturnsMessageAsDetail()
    {
        var context = CreateContext();

        await _handler.TryHandleAsync(context, new InvalidDocumentException("Title must not be empty."), CancellationToken.None);

        var problem = await ReadProblemAsync(context);
        Assert.Equal("Title must not be empty.", problem.Detail);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task TryHandleAsync_TechnicalError_DoesNotLeakInternalMessage()
    {
        var context = CreateContext();

        await _handler.TryHandleAsync(context,
            new DataAccessException("connection to db:5432 refused", new InvalidOperationException()), CancellationToken.None);

        var problem = await ReadProblemAsync(context);
        Assert.DoesNotContain("5432", problem.Detail);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/documents";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return problem!;
    }
}
