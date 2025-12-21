using Api.Shared.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Shared.Exceptions;

public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            DomainException domainEx => CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, domainEx.Message, domainEx.Code),
            ValidationException validationEx => CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Validation failed", "validation_error", validationEx.Errors.Select(e => e.ErrorMessage).ToArray()),
            _ => CreateProblemDetails(httpContext, StatusCodes.Status500InternalServerError, "Unexpected error", "unexpected_error")
        };

        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string code, string[]? errors = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = code,
            Instance = context.TraceIdentifier,
            Extensions = { ["errors"] = errors ?? Array.Empty<string>() }
        };
    }
}
