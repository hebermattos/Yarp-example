# Authentication

This project uses **JWT Bearer authentication** to protect the YARP reverse proxy routes.

The root endpoint (`/`) and the demo token endpoint (`/auth/token`) are public. The proxied routes require a valid Bearer token.

## Package

The project references:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

## Configuration

JWT settings are stored in `appsettings.json`:

```json
"Jwt": {
  "Issuer": "yarp-example",
  "Audience": "yarp-example-clients",
  "SigningKey": "CHANGE_ME_DEMO_SIGNING_KEY_32_CHARS_MINIMUM"
}
```

The signing key in this sample is only for local demo usage. For real applications, store secrets outside source control, for example in environment variables, user secrets, Azure Key Vault, GitHub Actions secrets, or another secure secret provider.

## Service registration

JWT Bearer authentication is registered in `Program.cs`:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSecurityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
```

The authentication and authorization middleware are enabled before rate limiting, output caching, and the reverse proxy endpoint:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
```

## Demo token endpoint

The project includes a simple local endpoint that generates a demo JWT:

```text
POST /auth/token
```

Example:

```bash
curl -X POST http://localhost:5000/auth/token
```

Example response shape:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-01-01T00:00:00Z"
}
```

## Protected proxy routes

The reverse proxy endpoint requires authorization:

```csharp
app.MapReverseProxy()
    .RequireAuthorization()
    .RequireRateLimiting("fixed-window")
    .CacheOutput("short-cache");
```

This means calls to proxied routes such as `/todos/1`, `/dogs/random`, and `/countries/name/brazil` require a valid JWT.

## Testing without a token

```bash
curl -i http://localhost:5000/todos/1
```

Expected result:

```text
HTTP/1.1 401 Unauthorized
```

## Testing with a token

Get a token:

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/auth/token | jq -r '.accessToken')
```

Call a protected proxied route:

```bash
curl -i -H "Authorization: Bearer $TOKEN" http://localhost:5000/todos/1
```

If `jq` is not available, copy the `accessToken` value manually and pass it in the `Authorization` header:

```bash
curl -i -H "Authorization: Bearer YOUR_TOKEN_HERE" http://localhost:5000/todos/1
```

## Production notes

The `/auth/token` endpoint is intentionally simple and should not be used as-is in production.

For production scenarios, consider:

- Using a real identity provider.
- Validating issuer and audience against production values.
- Storing signing keys securely.
- Rotating signing keys.
- Using asymmetric signing keys where appropriate.
- Adding authorization policies based on scopes, roles, or claims.
- Avoiding local token minting inside the gateway unless it is an intentional identity component.
