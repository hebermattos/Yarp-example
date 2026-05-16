# YARP Example

Minimal **ASP.NET Core 8** example using **YARP** (*Yet Another Reverse Proxy*) to create a gateway/reverse proxy.

The project shows how an ASP.NET Core application can receive requests on local routes and forward those requests to external APIs. It also includes **JWT Bearer authentication**, **Serilog**, **rate limiting**, and **output caching**.

## Features

- ASP.NET Core 8 minimal API
- YARP reverse proxy
- Three public upstream APIs
- Request path transforms
- JWT Bearer authentication for proxied routes
- Demo token endpoint for local testing
- Serilog structured logging
- Console logging in `DEBUG` builds
- File logging in `Development` and `Production`
- Fixed-window rate limiting
- Short output cache policy
- GitHub Actions build workflow

## Quick start

```bash
git clone https://github.com/hebermattos/Yarp-example.git
cd Yarp-example
dotnet restore
dotnet run --urls http://localhost:5000
```

Open the root endpoint:

```bash
curl http://localhost:5000/
```

Get a demo JWT:

```bash
curl -X POST http://localhost:5000/auth/token
```

## Public APIs used

| Local route | Public API | Real destination | Authentication |
|---|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` | Required |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` | Required |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` | Required |

## Test requests

Get a token and call a protected proxy route:

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/auth/token | jq -r '.accessToken')
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/todos/1
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/dogs/random
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/countries/name/brazil
```

Without a token, proxied routes return `401 Unauthorized`:

```bash
curl -i http://localhost:5000/todos/1
```

## Policies

| Policy | Current configuration |
|---|---|
| Authentication | JWT Bearer required for proxied routes |
| Rate limiting | 5 requests every 10 seconds, no queue |
| Output caching | 30 seconds, varies by query string |
| Logging | Console in `DEBUG`; file in `Development` and `Production` |

## Documentation

- [Getting started](docs/getting-started.md)
- [Authentication](docs/authentication.md)
- [YARP configuration](docs/yarp-configuration.md)
- [Logging](docs/logging.md)
- [Rate limiting and caching](docs/rate-limiting-and-caching.md)

## Project structure

```text
.
├── .github/
│   └── workflows/
│       └── build.yml
├── Properties/
│   └── launchSettings.json
├── docs/
│   ├── authentication.md
│   ├── getting-started.md
│   ├── logging.md
│   ├── rate-limiting-and-caching.md
│   └── yarp-configuration.md
├── .gitignore
├── Program.cs
├── README.md
├── YarpExample.csproj
└── appsettings.json
```

## Main files

| File | Purpose |
|---|---|
| `Program.cs` | Configures ASP.NET Core, JWT authentication, Serilog, rate limiting, output caching, and YARP. |
| `appsettings.json` | Defines JWT settings and YARP routes, clusters, destinations, and transforms. |
| `YarpExample.csproj` | Defines the target framework and package references. |
| `.github/workflows/build.yml` | Restores and builds the project in GitHub Actions. |

## Notes

This repository is intended as a small learning sample, not as a production-ready gateway template.

The implementation intentionally keeps the configuration simple and local to the application so the main YARP concepts are easy to inspect: routes, clusters, destinations, transforms, authentication, logging, rate limiting, and output caching.

Current limitations:

- Demo-only local token endpoint.
- Demo signing key stored in `appsettings.json`.
- No external identity provider integration.
- No distributed rate limiting.
- No distributed cache provider.
- No health checks for upstream destinations.
- No retry, circuit breaker, or resiliency pipeline.
- No OpenTelemetry or Application Insights integration.
- No Dockerfile or deployment-specific configuration.

For a production gateway, consider adding a real identity provider, authorization policies based on scopes or roles, secure secret management, health checks, resiliency, distributed cache/rate limiting, centralized observability, and environment-specific deployment settings.
