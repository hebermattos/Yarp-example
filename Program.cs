var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

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
