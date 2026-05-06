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

// --- Database Configuration (PostgreSQL/SQL Server Hybrid) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<RestaurantDbContext>(options =>
{
    if (connectionString!.Contains("Host="))
    {
        options.UseNpgsql(connectionString, npgsqlOptions => 
            npgsqlOptions.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions => 
            sqlOptions.EnableRetryOnFailure());
    }
});

// --- Redis Cache Configuration ---
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "QuickBite_";
});

// --- Authentication Configuration ---
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

// --- Dependency Injection ---
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger Configuration ---
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

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBite.Restaurant API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Database Migration & Seeding ---
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
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Pizza Palace",
                    Cuisine = "Pizza, Italian",
                    Address = "CP, Block A",
                    City = "New Delhi",
                    Latitude = 28.6139,
                    Longitude = 77.2090,
                    AvgRating = 4.5,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 30,
                    ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440011"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Burger Town",
                    Cuisine = "Burgers, Fast Food",
                    Address = "Saket District Center",
                    City = "New Delhi",
                    Latitude = 28.5244,
                    Longitude = 77.2167,
                    AvgRating = 4.3,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 25,
                    ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440012"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Royal Biryani House",
                    Cuisine = "Biryani, Mughlai",
                    Address = "Jama Masjid Area",
                    City = "New Delhi",
                    Latitude = 28.6507,
                    Longitude = 77.2334,
                    AvgRating = 4.8,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 45,
                    ImageUrl = "https://images.unsplash.com/photo-1563379091339-03b21bc4a4f8?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440013"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Grand Indian Thali",
                    Cuisine = "North Indian, Thali",
                    Address = "Rajouri Garden",
                    City = "New Delhi",
                    Latitude = 28.6415,
                    Longitude = 77.1209,
                    AvgRating = 4.6,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 40,
                    ImageUrl = "https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440014"),
                    OwnerId = Guid.NewGuid(),
                    Name = "Sweet Cravings",
                    Cuisine = "Desserts, Bakery",
                    Address = "Hauz Khas Village",
                    City = "New Delhi",
                    Latitude = 28.5528,
                    Longitude = 77.1903,
                    AvgRating = 4.9,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1551024506-0bccd828d307?w=800"
                },
                new QuickBite.Restaurant.Entities.Restaurant
                {
                    RestaurantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440015"),
                    OwnerId = Guid.NewGuid(),
                    Name = "The Health Hub",
                    Cuisine = "Healthy Food, Salads",
                    Address = "Cyber Hub",
                    City = "Gurugram",
                    Latitude = 28.4951,
                    Longitude = 77.0878,
                    AvgRating = 4.7,
                    IsOpen = true,
                    IsApproved = true,
                    EstimatedDeliveryMin = 35,
                    ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800"
                }
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
