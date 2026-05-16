using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
var jwtSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

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

app.UseAuthentication();
app.UseAuthorization();
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
    Security = new
    {
        Authentication = "JWT Bearer",
        TokenEndpoint = "/auth/token",
        ProtectedProxyRoutes = true
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

app.MapPost("/auth/token", (IConfiguration configuration) =>
{
    var issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    var audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
    var signingKey = configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    var expiresAt = DateTime.UtcNow.AddMinutes(30);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, "demo-user"),
        new Claim(JwtRegisteredClaimNames.Name, "Demo User"),
        new Claim("scope", "gateway.read")
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: expiresAt,
        signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        AccessToken = accessToken,
        TokenType = "Bearer",
        ExpiresAtUtc = expiresAt
    });
});

app.MapReverseProxy()
    .RequireAuthorization()
    .RequireRateLimiting("fixed-window")
    .CacheOutput("short-cache");

app.Run();
