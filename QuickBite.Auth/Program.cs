using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickBite.Auth.Data;
using QuickBite.Auth.Entities;
using QuickBite.Auth.Interfaces;
using QuickBite.Auth.Repositories;
using QuickBite.Auth.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- Database Configuration ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
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
        options.UseNpgsql(connUrl, npgsqlOptions => 
            npgsqlOptions.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlServer(connectionString!, sqlOptions => 
            sqlOptions.EnableRetryOnFailure());
    }
});

// --- Identity Configuration ---
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// --- Authentication & JWT Configuration ---
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

authBuilder.AddJwtBearer(options =>
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

// Only add Google if ClientId is provided
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });
}

// Only add GitHub if ClientId is provided
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
if (!string.IsNullOrEmpty(githubClientId))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
    });
}

// --- Register Services (DI) ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();

// --- Swagger Configuration ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuickBite.Auth API", Version = "v1" });
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
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBite.Auth API V1");
        c.RoutePrefix = string.Empty; // Set Swagger as the home page
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    for (int i = 0; i < 15; i++)
    {
        try
        {
            logger.LogInformation($"Attempting to migrate database (attempt {i + 1})...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
            
            logger.LogInformation("Seeding default auth data...");
            QuickBite.Auth.Data.DbInitializer.SeedAsync(dbContext, userManager).GetAwaiter().GetResult();
            logger.LogInformation("Auth data seeding completed.");
            break;
        }
        catch (Exception ex)
        {
            if (i == 14)
            {
                logger.LogError(ex, "Database migration failed after maximum retries.");
                throw;
            }
            logger.LogWarning($"Database migration failed: {ex.Message}. Retrying in 3 seconds...");
            System.Threading.Thread.Sleep(3000);
        }
    }
}

app.Run();
