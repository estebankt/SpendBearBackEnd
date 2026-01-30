using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SpendBear.SharedKernel;

namespace SpendBear.Api.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions
/// and returns structured error responses using ProblemDetails format.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = CreateProblemDetails(context, exception);

        // Log the exception with appropriate level
        LogException(exception, statusCode, context);

        // Set response content type and status code
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        // Serialize and write the response
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private (int StatusCode, ProblemDetails ProblemDetails) CreateProblemDetails(
        HttpContext context,
        Exception exception)
    {
        var traceId = context.TraceIdentifier;

        return exception switch
        {
            // Domain exceptions indicate business rule violations (client error)
            DomainException domainEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "Domain Rule Violation",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = domainEx.Message,
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            ),

            // Database connection exceptions (service unavailable)
            NpgsqlException npgsqlEx when IsTransientError(npgsqlEx) => (
                StatusCode: (int)HttpStatusCode.ServiceUnavailable,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                    Title = "Database Temporarily Unavailable",
                    Status = (int)HttpStatusCode.ServiceUnavailable,
                    Detail = _environment.IsDevelopment()
                        ? npgsqlEx.Message
                        : "The service is temporarily unavailable. Please try again later.",
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["retryAfter"] = "5"
                    }
                }
            ),

            // Other database exceptions (internal server error)
            NpgsqlException npgsqlEx => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    Title = "Database Error",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = _environment.IsDevelopment()
                        ? npgsqlEx.Message
                        : "An error occurred while processing your request.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            ),

            // General argument exceptions (bad request)
            ArgumentException argEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "Invalid Argument",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = argEx.Message,
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            ),

            // Invalid operation exceptions (conflict or internal error)
            InvalidOperationException invalidOpEx => (
                StatusCode: (int)HttpStatusCode.Conflict,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                    Title = "Invalid Operation",
                    Status = (int)HttpStatusCode.Conflict,
                    Detail = invalidOpEx.Message,
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            ),

            // Unauthorized access
            UnauthorizedAccessException _ => (
                StatusCode: (int)HttpStatusCode.Forbidden,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    Title = "Forbidden",
                    Status = (int)HttpStatusCode.Forbidden,
                    Detail = "You do not have permission to access this resource.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            ),

            // All other exceptions (internal server error)
            _ => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ProblemDetails: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    Title = "Internal Server Error",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = _environment.IsDevelopment()
                        ? $"{exception.Message}\n\nStack Trace:\n{exception.StackTrace}"
                        : "An unexpected error occurred while processing your request.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }
            )
        };
    }

    private void LogException(Exception exception, int statusCode, HttpContext context)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
            context.TraceIdentifier,
            context.Request.Path,
            context.Request.Method,
            statusCode);
    }

    private static bool IsTransientError(NpgsqlException ex)
    {
        // PostgreSQL error codes that indicate transient errors
        // https://www.postgresql.org/docs/current/errcodes-appendix.html
        var transientErrorCodes = new[]
        {
            "08000", // connection_exception
            "08003", // connection_does_not_exist
            "08006", // connection_failure
            "08001", // sqlclient_unable_to_establish_sqlconnection
            "08004", // sqlserver_rejected_establishment_of_sqlconnection
            "53300", // too_many_connections
            "57P03", // cannot_connect_now
            "58000", // system_error
            "58030"  // io_error
        };

        return ex.SqlState != null && transientErrorCodes.Contains(ex.SqlState);
    }
}
