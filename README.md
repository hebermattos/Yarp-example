# YARP Example

Minimal **ASP.NET Core 8** example using **YARP** (*Yet Another Reverse Proxy*) to create a gateway/reverse proxy.

The goal of this project is to show how an ASP.NET Core application can receive requests on local routes and forward those requests to external APIs, so the client does not need to know the real upstream destinations.

In addition to YARP, this project also includes **Serilog** for structured logging.

## What this example demonstrates

This project demonstrates:

- How to install and register the `Yarp.ReverseProxy` package.
- How to configure proxy routes through `appsettings.json`.
- How to map local routes to different public APIs.
- How to use `Routes`, `Clusters`, `Destinations`, and `Transforms`.
- How to expose a simple ASP.NET Core application as an API Gateway.
- How to configure Serilog in ASP.NET Core.
- How to log to the console when the project is built in `DEBUG` mode.
- How to write logs to files in the `Development` and `Production` environments.

## Public APIs used

The gateway exposes three local routes and forwards each one to a different public API:

| Local route | Public API | Real destination |
|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` |

## Requirements

- .NET 8 SDK
- Git
- Terminal, PowerShell, Bash, or similar shell

## How to run

Clone the repository:

```bash
git clone https://github.com/hebermattos/Yarp-example.git
cd Yarp-example
```

Restore the packages:

```bash
dotnet restore
```

Run the application:

```bash
dotnet run
```

Or force a specific URL:

```bash
dotnet run --urls http://localhost:5000
```

## How to test

Open the root endpoint to see a summary of the available routes:

```bash
curl http://localhost:5000/
```

Test the todos API:

```bash
curl http://localhost:5000/todos/1
```

This request enters the gateway at:

```text
http://localhost:5000/todos/1
```

YARP forwards it to:

```text
https://jsonplaceholder.typicode.com/todos/1
```

Test the random dog image API:

```bash
curl http://localhost:5000/dogs/random
```

This request enters the gateway at:

```text
http://localhost:5000/dogs/random
```

YARP forwards it to:

```text
https://dog.ceo/api/breeds/image/random
```

Test the countries API:

```bash
curl http://localhost:5000/countries/name/brazil
```

This request enters the gateway at:

```text
http://localhost:5000/countries/name/brazil
```

YARP forwards it to:

```text
https://restcountries.com/v3.1/name/brazil
```

## How YARP is registered

In `Program.cs`, YARP is registered in the application's dependency injection container:

```csharp
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

This does two important things:

1. `AddReverseProxy()` adds the services required by YARP.
2. `LoadFromConfig(...)` tells YARP to read its configuration from the `ReverseProxy` section in `appsettings.json`.

Then the proxy is mapped into the HTTP pipeline:

```csharp
app.MapReverseProxy();
```

This makes requests matching the configured routes handled by YARP.

## How Serilog is configured

The project uses Serilog at the ASP.NET Core host level:

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

### Applied logging rule

| Condition | Log output |
|---|---|
| `DEBUG` build | Console |
| `Development` environment | File under `logs/` |
| `Production` environment | File under `logs/` |
| Other environments | No file output unless also built in `DEBUG` mode |

### Console only in DEBUG

The console sink is registered inside a compilation directive:

```csharp
#if DEBUG
    loggerConfiguration.WriteTo.Console();
#endif
```

This means the console sink is only included in the binary when the project is compiled in `Debug` mode.

### File logging in Development and Production

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

The configuration uses:

- `rollingInterval: RollingInterval.Day`: creates one log file per day.
- `retainedFileCountLimit: 14`: keeps up to 14 log files.
- `restrictedToMinimumLevel: LogEventLevel.Information`: writes logs starting from `Information`.

The `logs/` folder is included in `.gitignore` to avoid committing generated log files.

### HTTP request logging

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

## Core YARP concepts

### Route

A `Route` defines **which local request should be captured** by the gateway.

Example:

```json
"todos-route": {
  "ClusterId": "jsonplaceholder-cluster",
  "Match": {
    "Path": "/todos/{**catch-all}"
  }
}
```

In this case, any request starting with `/todos/` is captured by this route.

Examples:

```text
/todos/1
/todos/10
/todos/99
```

### Cluster

A `Cluster` defines **where the request should be forwarded**.

Example:

```json
"jsonplaceholder-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "https://jsonplaceholder.typicode.com/"
    }
  }
}
```

A route points to a cluster through `ClusterId`:

```json
"ClusterId": "jsonplaceholder-cluster"
```

The flow is:

```text
Route -> Cluster -> Destination
```

### Destination

A `Destination` is the final upstream address where YARP sends the request.

Example:

```json
"Address": "https://jsonplaceholder.typicode.com/"
```

A cluster can have one or more destinations. In real-world scenarios, this allows load balancing between multiple instances of the same API.

### Transform

A `Transform` allows changing parts of the request before forwarding it to the upstream destination.

In this example, the local `/dogs/random` route is converted to the real Dog CEO API path:

```json
"Transforms": [
  {
    "PathSet": "/api/breeds/image/random"
  }
]
```

So the client calls:

```text
/dogs/random
```

But the upstream destination receives:

```text
/api/breeds/image/random
```

The example also uses `PathPattern` to preserve part of the captured route:

```json
"PathPattern": "/v3.1/{**catch-all}"
```

With this transform:

```text
/countries/name/brazil
```

becomes:

```text
/v3.1/name/brazil
```

## Request flow

Example using `/countries/name/brazil`:

```text
Client
  ↓
GET http://localhost:5000/countries/name/brazil
  ↓
ASP.NET Core receives the request
  ↓
YARP matches the countries-route route
  ↓
YARP applies the PathPattern transform
  ↓
YARP forwards the request to https://restcountries.com/v3.1/name/brazil
  ↓
The response returns through YARP
  ↓
The client receives the response
```

## Project structure

```text
.
├── .github/
│   └── workflows/
│       └── build.yml
├── Properties/
│   └── launchSettings.json
├── .gitignore
├── Program.cs
├── README.md
├── YarpExample.csproj
└── appsettings.json
```

## Main files

### `YarpExample.csproj`

Contains the package references used by the project:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
```

### `Program.cs`

Configures the ASP.NET Core application, registers Serilog, registers YARP, and maps the reverse proxy.

### `appsettings.json`

Contains all route, cluster, destination, and transform configuration.

### `.github/workflows/build.yml`

Simple GitHub Actions workflow that restores and builds the project on each push or pull request targeting the `main` branch.

## How to add a new API

To add a new API, create:

1. A new route under `ReverseProxy:Routes`.
2. A new cluster under `ReverseProxy:Clusters`.
3. A destination pointing to the real API.

Conceptual example:

```json
"my-api-route": {
  "ClusterId": "my-api-cluster",
  "Match": {
    "Path": "/my-api/{**catch-all}"
  },
  "Transforms": [
    {
      "PathPattern": "/{**catch-all}"
    }
  ]
}
```

And the cluster:

```json
"my-api-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "https://example.com/"
    }
  }
}
```

## When to use YARP

YARP is useful when you need a .NET gateway for scenarios such as:

- API Gateway for multiple services.
- Reverse proxy for internal APIs.
- Centralized routing.
- Gradual migration between legacy systems and new services.
- Applying authentication, authorization, headers, rate limiting, or observability in a single place.
- Load balancing between multiple destinations.

## Notes

This project is intentionally simple. It does not implement authentication, rate limiting, caching, health checks, or advanced observability.

The goal is to provide a starting point for understanding the basic YARP configuration in an ASP.NET Core application with Serilog.
