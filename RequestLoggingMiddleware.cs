using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Generate a short, lightweight 8-character correlation tracking identifier
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        // 2. Inject the tracking header into the response collection early before downstream components write to the stream
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // 3. Document the incoming request details on entry
        _logger.LogInformation(
            "Request Started | ID: {CorrelationId} | Method: {Method} | Path: {Path}",
            correlationId, context.Request.Method, context.Request.Path);

        // 4. Initialize a high-resolution stopwatch to accurately profile pipeline duration
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Pass control to the next middleware or terminal endpoint down the line
            await _next(context);
        }
        finally
        {
            // 5. Halt timing and capture the exact elapsed duration, even if an exception was thrown downstream
            stopwatch.Stop();
            
            // 6. Log the completion details on exit, mapping back to the identical correlation tracking ID
            _logger.LogInformation(
                "Request Finished | ID: {CorrelationId} | Status: {StatusCode} | Duration: {ElapsedMs}ms",
                correlationId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}