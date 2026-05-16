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

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.MapGet("/", () => Results.Ok(new
{
    Application = "YARP minimal gateway example",
    Routes = new[]
    {
        "/todos/{**catch-all} -> JSONPlaceholder",
        "/dogs/random -> Dog CEO",
        "/countries/{**catch-all} -> REST Countries"
    },
    Examples = new[]
    {
        "/todos/1",
        "/dogs/random",
        "/countries/name/brazil"
    }
}));

app.MapReverseProxy();

app.Run();
