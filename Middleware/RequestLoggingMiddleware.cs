using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogiTrack.Middleware
{
    /// <summary>
    /// Middleware that logs request start and end times with elapsed duration.
    /// Useful for basic request telemetry during reviews and troubleshooting.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Logs request information before and after executing the rest of the pipeline.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var path = context.Request.Path + context.Request.QueryString;
            var method = context.Request.Method;

            _logger.LogInformation("Handling request {Method} {Path}", method, path);

            await _next(context);

            sw.Stop();
            var status = context.Response?.StatusCode;
            _logger.LogInformation("Finished request {Method} {Path} responded {Status} in {Elapsed}ms", method, path, status, sw.ElapsedMilliseconds);
        }
    }
}
