# Rate Limiting and Caching

This project uses ASP.NET Core rate limiting and output caching applied to the YARP reverse proxy endpoint.

## Rate limiting

The project uses a fixed-window rate limiter.

Configuration in `Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                Error = "Too many requests.",
                Detail = "The fixed-window rate limit was exceeded. Try again later."
            },
            cancellationToken);
    };

    options.AddFixedWindowLimiter("fixed-window", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});
```

Current rule:

| Policy | Value |
|---|---|
| Policy name | `fixed-window` |
| Limit | 5 requests |
| Window | 10 seconds |
| Queue | Disabled |
| Rejection status | `429 Too Many Requests` |

The middleware is enabled with:

```csharp
app.UseRateLimiter();
```

The policy is applied to the reverse proxy endpoint:

```csharp
app.MapReverseProxy()
    .RequireRateLimiting("fixed-window");
```

## Testing rate limiting

Run several requests quickly:

```bash
for i in {1..10}; do curl -i http://localhost:5000/todos/1; echo; done
```

After 5 requests within 10 seconds, the gateway should start returning:

```text
HTTP/1.1 429 Too Many Requests
```

The custom rejection response is JSON:

```json
{
  "error": "Too many requests.",
  "detail": "The fixed-window rate limit was exceeded. Try again later."
}
```

## Output caching

The project uses ASP.NET Core output caching with a short cache policy.

Configuration in `Program.cs`:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("short-cache", policyBuilder =>
    {
        policyBuilder
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("*");
    });
});
```

Current rule:

| Policy | Value |
|---|---|
| Policy name | `short-cache` |
| Duration | 30 seconds |
| Query string variation | All query string values |

The middleware is enabled with:

```csharp
app.UseOutputCache();
```

The policy is applied to the reverse proxy endpoint:

```csharp
app.MapReverseProxy()
    .CacheOutput("short-cache");
```

## Testing output caching

Call the same endpoint multiple times within 30 seconds:

```bash
curl -i http://localhost:5000/todos/1
curl -i http://localhost:5000/todos/1
```

The second call may be served from the gateway cache instead of calling the upstream API again, depending on whether the response is cacheable according to ASP.NET Core output caching rules.

For an endpoint with changing upstream content, try:

```bash
curl -i http://localhost:5000/dogs/random
curl -i http://localhost:5000/dogs/random
```

Within the cache window, repeated calls can return the cached response.

## Notes

This example uses in-memory rate limiting and in-memory output caching. For multi-instance production deployments, consider distributed strategies so all gateway instances share consistent limits and cache behavior.
