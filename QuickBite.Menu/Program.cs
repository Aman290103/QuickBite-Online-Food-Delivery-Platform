using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickBite.Menu.Data;
using QuickBite.Menu.Helpers;
using QuickBite.Menu.Interfaces;
using QuickBite.Menu.Repositories;
using QuickBite.Menu.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Database Configuration (Hybrid) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MenuDbContext>(options =>
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

// --- Redis Cache ---
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "QuickBiteMenu_";
});

// --- AWS S3 Configuration ---
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IS3UploadHelper, S3UploadHelper>();

// --- Authentication ---
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
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IMenuService, MenuService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuickBite.Menu API", Version = "v1" });
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBite.Menu API V1");
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
        var context = services.GetRequiredService<MenuDbContext>();
        context.Database.Migrate();

        if (!context.Categories.Any())
        {
            // Brijwasi Sweets
            var res1Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440020");
            var cat1 = new QuickBite.Menu.Entities.MenuCategory { CategoryId = Guid.NewGuid(), RestaurantId = res1Id, Name = "Sweets", Description = "Authentic Mathura Sweets" };
            context.Categories.Add(cat1);
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res1Id, CategoryId = cat1.CategoryId, Name = "Mathura Peda", Description = "The famous Mathura Peda", Price = 250, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1589113103503-49ef83d89e7c?w=400" });
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res1Id, CategoryId = cat1.CategoryId, Name = "Gulab Jamun", Description = "Soft berry-sized balls in syrup", Price = 120, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1591206369811-4eeb2f03bc95?w=400" });

            // Shankar North Indian
            var res2Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440021");
            var cat2 = new QuickBite.Menu.Entities.MenuCategory { CategoryId = Guid.NewGuid(), RestaurantId = res2Id, Name = "Main Course", Description = "Pure Veg North Indian" };
            context.Categories.Add(cat2);
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res2Id, CategoryId = cat2.CategoryId, Name = "Paneer Butter Masala", Description = "Creamy cottage cheese curry", Price = 320, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1601050690597-df0568f70950?w=400" });
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res2Id, CategoryId = cat2.CategoryId, Name = "Dal Makhani", Description = "Creamy black lentils", Price = 280, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=400" });

            // Pizza Hut Fast Food
            var res3Id = Guid.Parse("770e8400-e29b-41d4-a716-446655440022");
            var cat3 = new QuickBite.Menu.Entities.MenuCategory { CategoryId = Guid.NewGuid(), RestaurantId = res3Id, Name = "Pizzas", Description = "Delicious Veg Pizzas" };
            context.Categories.Add(cat3);
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res3Id, CategoryId = cat3.CategoryId, Name = "Margherita Pizza", Description = "Classic cheese pizza", Price = 399, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38?w=400" });
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = res3Id, CategoryId = cat3.CategoryId, Name = "Veggie Supreme", Description = "Loaded with fresh vegetables", Price = 499, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400" });
            
            // Haldiram's Delhi (Generic ID match logic or just adding more)
            var cat4 = new QuickBite.Menu.Entities.MenuCategory { CategoryId = Guid.NewGuid(), RestaurantId = Guid.NewGuid(), Name = "Snacks", Description = "Famous Delhi Snacks" };
            context.Categories.Add(cat4);
            context.Items.Add(new QuickBite.Menu.Entities.MenuItem { ItemId = Guid.NewGuid(), RestaurantId = cat4.RestaurantId, CategoryId = cat4.CategoryId, Name = "Chole Bhature", Description = "Spicy chickpeas with fried bread", Price = 180, IsVeg = true, ImageUrl = "https://images.unsplash.com/photo-1626132646545-0d3290610313?w=400" });

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
