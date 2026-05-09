using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickBite.Restaurant.Data;
using QuickBite.Restaurant.Interfaces;
using QuickBite.Restaurant.Repositories;
using QuickBite.Restaurant.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Database Configuration (Surgical Fix) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<RestaurantDbContext>(options =>
{
    var connUrl = connectionString;
    if (connUrl != null && (connUrl.StartsWith("postgres://") || connUrl.StartsWith("postgresql://")))
    {
        connUrl = connUrl.Replace("postgresql://", "postgres://");
        connUrl = connUrl.Replace("postgres://", "");
        var userPassSide = connUrl.Split('@')[0];
        var hostDbSide = connUrl.Split('@')[1];
        var user = userPassSide.Split(':')[0];
        var pass = userPassSide.Split(':')[1];
        var hostSide = hostDbSide.Split('/')[0];
        var db = hostDbSide.Split('/')[1].Split('?')[0];
        var host = hostSide.Split(':')[0];
        var port = hostSide.Contains(":") ? hostSide.Split(':')[1] : "5432";
        connUrl = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }

    if (connUrl != null && connUrl.Contains("Host="))
    {
        options.UseNpgsql(connUrl, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlServer(connectionString!, sqlOptions => sqlOptions.EnableRetryOnFailure());
    }
});

// --- Redis ---
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "QuickBite_";
});

// --- Auth ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// --- DI ---
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuickBite.Restaurant API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Automatic Seeding (Restoring Your Data) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RestaurantDbContext>();
        context.Database.Migrate();

        if (!context.Restaurants.Any())
        {
            context.Restaurants.AddRange(new List<QuickBite.Restaurant.Entities.Restaurant>
            {
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("770e8400-e29b-41d4-a716-446655440020"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Brijwasi Mithai Wala",
                    Cuisine = "Sweets, Veg North Indian",
                    Address = "Holi Gate",
                    City = "Mathura",
                    Latitude = 27.4924,
                    Longitude = 77.6737,
                    AvgRating = 4.9,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1589113103503-49ef83d89e7c?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("770e8400-e29b-41d4-a716-446655440021"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Shankar Mithai Wala",
                    Cuisine = "North Indian, Pure Veg",
                    Address = "Krishna Nagar",
                    City = "Mathura",
                    Latitude = 27.5010,
                    Longitude = 77.6690,
                    AvgRating = 4.7,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 25,
                    ImageUrl = "https://images.unsplash.com/photo-1601050690597-df0568f70950?w=800"
                }
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
