using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Logging (correlation ID) ---
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// --- 2. YARP Reverse Proxy ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpClient();

// --- 3. Central JWT Authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// --- 4. Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Public Rate Limit (100 req/min per IP)
    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 2;
    });

    // Authenticated Rate Limit (500 req/min per User)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var user = httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(user, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = httpContext.User.Identity?.IsAuthenticated == true ? 500 : 100,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

// --- 5. CORS (Angular Origin) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- 6. Swagger Aggregation ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuickBite API Gateway", Version = "v1" });
});

var app = builder.Build();

// --- Middleware Pipeline ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Aggregate Downstream Swagger Docs
        c.SwaggerEndpoint("/api/v1/auth/swagger/v1/swagger.json", "Auth Service");
        c.SwaggerEndpoint("/api/v1/restaurants/swagger/v1/swagger.json", "Restaurant Service");
        c.SwaggerEndpoint("/api/v1/menu/swagger/v1/swagger.json", "Menu Service");
        c.SwaggerEndpoint("/api/v1/cart/swagger/v1/swagger.json", "Cart Service");
        c.SwaggerEndpoint("/api/v1/orders/swagger/v1/swagger.json", "Order Service");
        c.SwaggerEndpoint("/api/v1/payments/swagger/v1/swagger.json", "Payment Service");
        c.SwaggerEndpoint("/api/v1/agents/swagger/v1/swagger.json", "Delivery Service");
        c.SwaggerEndpoint("/api/v1/notifications/swagger/v1/swagger.json", "Notification Service");
    });
}

// Add Correlation ID to Log Context
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await next();
    }
});

app.UseWebSockets(); // Crucial for SignalR proxying
app.UseCors("AngularPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Aggregate Health Check
app.MapGet("/health", async (IHttpClientFactory factory) =>
{
    var services = new[] { 
        ("auth-service", 8080), ("restaurant-service", 8080), ("menu-service", 8080), ("cart-service", 8080), 
        ("order-service", 8080), ("payment-service", 8080), ("delivery-service", 8080), ("notification-service", 8080) 
    };

    var client = factory.CreateClient();
    var results = new Dictionary<string, string>();

    foreach (var (name, port) in services)
    {
        try {
            var response = await client.GetAsync($"http://{name}:{port}/health");
            results[name] = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy";
        } catch { results[name] = "Down"; }
    }

    return Results.Ok(new { Status = "Gateway Up", Downstream = results });
});

app.MapReverseProxy();

app.Run();
