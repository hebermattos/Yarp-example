using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Yarp.ReverseProxy", LogEventLevel.Information);

#if DEBUG
    loggerConfiguration.WriteTo.Console();
#endif

    if (context.HostingEnvironment.IsDevelopment() || context.HostingEnvironment.IsProduction())
    {
        loggerConfiguration.WriteTo.File(
            path: "logs/yarp-example-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            restrictedToMinimumLevel: LogEventLevel.Information);
    }
});

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

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("short-cache", policyBuilder =>
    {
        policyBuilder
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("*");
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseRateLimiter();
app.UseOutputCache();

app.MapGet("/", () => Results.Ok(new
{
    Application = "YARP minimal gateway example",
    Routes = new[]
    {
        "/todos/{**catch-all} -> JSONPlaceholder",
        "/dogs/random -> Dog CEO",
        "/countries/{**catch-all} -> REST Countries"
    },
    Policies = new
    {
        RateLimiter = "fixed-window: 5 requests every 10 seconds, no queue",
        OutputCache = "short-cache: 30 seconds, varies by query string"
    },
    Examples = new[]
    {
        "/todos/1",
        "/dogs/random",
        "/countries/name/brazil"
    }
}));

app.MapReverseProxy()
    .RequireRateLimiting("fixed-window")
    .CacheOutput("short-cache");

app.Run();
