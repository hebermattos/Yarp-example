# Logging

This project uses **Serilog** for structured logging.

## Packages

The project references:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
```

## Host configuration

Serilog is configured at the ASP.NET Core host level:

```csharp
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
```

## Applied logging rule

| Condition | Log output |
|---|---|
| `DEBUG` build | Console |
| `Development` environment | File under `logs/` |
| `Production` environment | File under `logs/` |
| Other environments | No file output unless also built in `DEBUG` mode |

## Console only in DEBUG

The console sink is registered inside a compilation directive:

```csharp
#if DEBUG
    loggerConfiguration.WriteTo.Console();
#endif
```

This means the console sink is only included in the binary when the project is compiled in `Debug` mode.

## File logging in Development and Production

The file sink is enabled at runtime when the environment is `Development` or `Production`:

```csharp
if (context.HostingEnvironment.IsDevelopment() || context.HostingEnvironment.IsProduction())
{
    loggerConfiguration.WriteTo.File(...);
}
```

Log files are generated under:

```text
logs/yarp-example-YYYYMMDD.log
```

The file sink uses:

- `rollingInterval: RollingInterval.Day`: creates one log file per day.
- `retainedFileCountLimit: 14`: keeps up to 14 log files.
- `restrictedToMinimumLevel: LogEventLevel.Information`: writes logs starting from `Information`.

The `logs/` folder is included in `.gitignore` to avoid committing generated log files.

## HTTP request logging

The project also uses:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
```

This middleware creates structured logs for HTTP requests processed by the application, including requests handled by YARP.

Example log message:

```text
HTTP GET /todos/1 responded 200 in 123.4567 ms
```
