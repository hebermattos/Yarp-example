# YARP Example

Minimal **ASP.NET Core 8** example using **YARP** (*Yet Another Reverse Proxy*) to create a gateway/reverse proxy.

The project shows how an ASP.NET Core application can receive requests on local routes and forward those requests to external APIs. It also includes **Serilog**, **rate limiting**, and **output caching**.

## Features

- ASP.NET Core 8 minimal API
- YARP reverse proxy
- Three public upstream APIs
- Request path transforms
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

## Public APIs used

| Local route | Public API | Real destination |
|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` |

## Test requests

```bash
curl http://localhost:5000/todos/1
curl http://localhost:5000/dogs/random
curl http://localhost:5000/countries/name/brazil
```

## Policies

| Policy | Current configuration |
|---|---|
| Rate limiting | 5 requests every 10 seconds, no queue |
| Output caching | 30 seconds, varies by query string |
| Logging | Console in `DEBUG`; file in `Development` and `Production` |

## Documentation

- [Getting started](docs/getting-started.md)
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
| `Program.cs` | Configures ASP.NET Core, Serilog, rate limiting, output caching, and YARP. |
| `appsettings.json` | Defines YARP routes, clusters, destinations, and transforms. |
| `YarpExample.csproj` | Defines the target framework and package references. |
| `.github/workflows/build.yml` | Restores and builds the project in GitHub Actions. |

## Notes

This project is intentionally simple. It does not implement authentication, distributed rate limiting, distributed caching, health checks, or advanced observability.

The goal is to provide a clean starting point for understanding YARP in an ASP.NET Core application.
