# YARP Example

Minimal ASP.NET Core 8 reverse proxy using [YARP](https://microsoft.github.io/reverse-proxy/).

The gateway exposes local routes and forwards them to three public APIs:

| Local route | Public API | Destination |
|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` |

## Requirements

- .NET 8 SDK

## Run

```bash
dotnet restore
dotnet run
```

You can force a specific URL:

```bash
dotnet run --urls http://localhost:5000
```

## Test

Open the root endpoint:

```bash
curl http://localhost:5000/
```

Test the proxied APIs:

```bash
curl http://localhost:5000/todos/1
curl http://localhost:5000/dogs/random
curl http://localhost:5000/countries/name/brazil
```

## How it works

The proxy is registered in `Program.cs`:

```csharp
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

Routes and clusters are configured in `appsettings.json` under the `ReverseProxy` section.

## Project structure

```text
.
├── Program.cs
├── appsettings.json
├── YarpExample.csproj
└── README.md
```
