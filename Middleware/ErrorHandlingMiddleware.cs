using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogiTrack.Middleware
{
    /// <summary>
    /// Global error handling middleware. Catches unhandled exceptions and returns
    /// a consistent problem+json response while logging the exception with correlation id.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the next middleware and catches any exceptions to transform them
        /// into an RFC 7807 problem+json response.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlation = context.Items.ContainsKey("CorrelationId") ? context.Items["CorrelationId"]?.ToString() : null;
                _logger.LogError(ex, "Unhandled exception (CorrelationId={CorrelationId})", correlation);

                var problem = new
                {
                    type = "https://httpstatuses.com/500",
                    title = "An unexpected error occurred.",
                    status = (int)HttpStatusCode.InternalServerError,
                    detail = "An internal server error occurred.",
                    correlationId = correlation
                };

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var json = JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
