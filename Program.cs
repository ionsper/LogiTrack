
using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;

// Configure Serilog early so startup logs are captured and file sink is ready.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} (CorrelationId: {CorrelationId}){NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();


// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register LogiTrackContext for dependency injection
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));


// Add Identity services with strong password policy
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<LogiTrackContext>();

// JWT Authentication configuration (register AFTER Identity so JWT becomes the default scheme)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing configuration: Jwt:Key")))
    };
});


// Add controller services with JSON options to ignore cycles
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Add in-memory caching
builder.Services.AddMemoryCache();

var app = builder.Build();


// Ensure database is created, migrations are applied, and roles are seeded (async)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogiTrack.Data.LogiTrackContext>();
    await db.Database.MigrateAsync();

    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "User", "Manager" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}


// HSTS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Correlation ID -> Error handling -> Request logging
app.UseMiddleware<LogiTrack.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<LogiTrack.Middleware.ErrorHandlingMiddleware>();
app.UseMiddleware<LogiTrack.Middleware.RequestLoggingMiddleware>();

// Enable authentication
app.UseAuthentication();

// Session cookie middleware (IMemoryCache-based). Populate User when no other authentication present.
app.Use(async (context, next) =>
{
    try
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            var sessionId = context.Request.Cookies[CacheKeys.SessionCookieName];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
                if (cache != null)
                {
                    if (cache.TryGetValue(CacheKeys.SessionKey(sessionId), out object cached) && cached is LogiTrack.Models.SessionInfo info)
                    {
                        var claims = new List<System.Security.Claims.Claim>
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, info.UserId)
                        };
                        foreach (var role in info.Roles ?? Array.Empty<string>())
                        {
                            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                        }

                        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "Session"));
                    }
                }
            }
        }
    }
    catch
    {
        // swallow and continue; authentication will run as configured
    }
    await next();
});

// Enable authorization
app.UseAuthorization();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogiTrack API v1");
    c.RoutePrefix = string.Empty;
});


// Use attribute routing only
app.MapControllers();

app.Run();


