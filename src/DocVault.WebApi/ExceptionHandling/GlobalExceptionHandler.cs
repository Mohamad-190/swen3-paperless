using DocVault.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.WebApi.ExceptionHandling;

// Zentrale Fehlerbehandlung: uebersetzt die Exceptions der einzelnen Schichten in
// HTTP-Antworten (ProblemDetails), damit die Controller keine try/catch-Bloecke brauchen.
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);
        var request = httpContext.Request;

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "{Method} {Path} failed with {StatusCode}", request.Method, request.Path, status);
        else
            _logger.LogWarning("{Method} {Path} rejected with {StatusCode}: {Reason}", request.Method, request.Path, status, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    // Fachliche Fehler geben ihre Meldung weiter, technische Fehler nur eine allgemeine,
    // damit keine Interna (SQL, Hostnamen, Stacktraces) beim Client landen.
    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
        BusinessValidationException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
        MessagingException => (StatusCodes.Status503ServiceUnavailable, "Service unavailable",
            "The document was saved but could not be queued for processing. Please try again later."),
        DataAccessException => (StatusCodes.Status500InternalServerError, "Database error",
            "A database error occurred. Please try again later."),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.")
    };
}
