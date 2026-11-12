using AeShipManagement.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Errors;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = ToProblemDetails(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request {Method} {Path} failed with {StatusCode}: {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    internal static ProblemDetails ToProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        },
        InvalidRequestException => Problem(StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
        ResourceNotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
        ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict", exception.Message),
        BusinessRuleException => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception.Message),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected error",
            "An unexpected error occurred. Use the traceId when reporting this issue.")
    };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
