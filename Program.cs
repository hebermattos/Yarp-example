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
        "/todos/{**catch-all} -> JSONPlaceholder Todos API",
        "/posts/{**catch-all} -> JSONPlaceholder Posts API",
        "/users/{**catch-all} -> JSONPlaceholder Users API"
    },
    Examples = new[]
    {
        "/todos/1",
        "/posts/1",
        "/users/1"
    }
}));

app.MapReverseProxy();

app.Run();
