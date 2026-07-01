var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();
    var isAllowed = !string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin);

    // Preflight OPTIONS — responde imediatamente sem passar ao YARP
    if (context.Request.Method == HttpMethods.Options)
    {
        if (isAllowed)
        {
            context.Response.Headers["Access-Control-Allow-Origin"]  = origin;
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH";
            context.Response.Headers["Vary"] = "Origin";
        }
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    // Para requests reais: OnStarting garante que os headers são escritos
    // DEPOIS do YARP processar a resposta, mas ANTES de enviar ao browser
    if (isAllowed)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Vary"] = "Origin";
            return Task.CompletedTask;
        });
    }

    await next();
});

app.MapReverseProxy();

app.Run();
