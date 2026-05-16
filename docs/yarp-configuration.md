# YARP Configuration

This document explains how YARP is configured in this project.

## Registration

In `Program.cs`, YARP is registered in the application's dependency injection container:

```csharp
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

This does two important things:

1. `AddReverseProxy()` adds the services required by YARP.
2. `LoadFromConfig(...)` tells YARP to read its configuration from the `ReverseProxy` section in `appsettings.json`.

The proxy is mapped into the HTTP pipeline:

```csharp
app.MapReverseProxy()
    .RequireRateLimiting("fixed-window")
    .CacheOutput("short-cache");
```

This makes requests matching the configured routes handled by YARP, while also applying rate limiting and output caching.

## Core concepts

### Route

A `Route` defines which local request should be captured by the gateway.

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

A `Cluster` defines where the request should be forwarded.

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
Rate limiter checks the request
  ↓
Output cache checks for a cached response
  ↓
YARP matches the countries-route route
  ↓
YARP applies the PathPattern transform
  ↓
YARP forwards the request to https://restcountries.com/v3.1/name/brazil
  ↓
The response returns through YARP
  ↓
Output cache stores the response when it is cacheable
  ↓
The client receives the response
```

## Adding a new API

To add a new API, create:

1. A new route under `ReverseProxy:Routes`.
2. A new cluster under `ReverseProxy:Clusters`.
3. A destination pointing to the real API.

Conceptual route example:

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

Conceptual cluster example:

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
- Applying authentication, authorization, headers, rate limiting, caching, or observability in a single place.
- Load balancing between multiple destinations.
