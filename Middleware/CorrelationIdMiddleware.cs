using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogiTrack.Middleware
{
    /// <summary>
    /// Middleware that ensures each request has a correlation id (X-Correlation-ID) for observability.
    /// Adds the id to response headers and the logging scope so requests can be correlated in logs.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Assigns or propagates a correlation id for the current request and invokes the next middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            const string headerName = "X-Correlation-ID";
            string correlationId = context.Request.Headers.ContainsKey(headerName)
                ? context.Request.Headers[headerName].ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers[headerName] = correlationId;
            context.Items["CorrelationId"] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }
    }
}
