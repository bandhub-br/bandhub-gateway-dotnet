using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("gateway-policy", httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        //Autenticado limita por usuario
        //Caso contrario, limita por IP
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity!.Name
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";


        //Login
        if (path.StartsWith("/auth/login"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"login-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //Register
        if (path.StartsWith("/auth/register"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"register-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //Auth (demais endpoints)
        if (path.StartsWith("/auth"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"auth-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //Accounts
        if (path.StartsWith("/accounts"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"accounts-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //Bands
        if (path.StartsWith("/bands"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"bands-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //BFF
        if (path.StartsWith("/bff"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"bff-{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 50,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        //Default
        return RateLimitPartition.GetFixedWindowLimiter($"default-{partitionKey}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

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

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapReverseProxy().RequireRateLimiting("gateway-policy");

app.Run();
