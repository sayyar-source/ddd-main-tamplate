using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using PrintBridge.Infrastructure.Data;
using PrintBridge.Infrastructure.Data.Extensions;
using PrintBridge.WebApi.Extensions;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
        new Uri(builder.Configuration["ElasticsearchUrl"] ?? "http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "supplier-portal-logs-{0:yyyy.MM.dd}"
    })
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PrintBridge.WebApi")
    .CreateLogger();

builder.Host.UseSerilog();
// ========================================
// Get Configuration Values
// ========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(";") ?? new[] { "http://localhost:7172" };

// ========================================
// Add Core Services
// ========================================
builder.Services.AddControllers();

// Database Context with retry policy
builder.Services.AddDbContext<PrintBridgeDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application Services (includes repositories, validators, mappings, Unit of Work)
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Supplier Service API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,  
        Scheme = "Bearer",  
        BearerFormat = "JWT" 
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"];
var key = Encoding.UTF8.GetBytes(secretKey);

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
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization();

// ========================================
// Add CORS Policy
// ========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// ========================================
// Apply Database Migrations
// ========================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PrintBridgeDbContext>();
    try
    {
       // Apply pending migrations
        Log.Information("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");

        Log.Information("Initializing database...");
        await app.Services.InitializeDatabaseAsync();
        Log.Information("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations");
        throw;
    }
}

// ========================================
// Configure HTTP Pipeline
// ========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Supplier Portal API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// GET /health — printer health check (bonus)
app.MapGet("/health", (PrintBridge.Infrastructure.Services.Printer.PrinterManager printer) =>
{
    var status = printer.GetStatus();
    var healthy = status.IsConnected && status.PaperStatus != "out" && status.CoverStatus == "closed";
    return healthy
        ? Results.Ok(new { status = "healthy", printer = status })
        : Results.Json(new { status = "degraded", printer = status }, statusCode: 503);
}).WithTags("Health");

// ========================================
// AUTO-OPEN SWAGGER WHEN APP STARTS
// ========================================
app.Lifetime.ApplicationStarted.Register(() =>
{
try
{
var server = app.Services.GetRequiredService<IServer>();
var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses;

foreach (var address in addresses)
{
var url = $"{address}/swagger/index.html";

Process.Start(new ProcessStartInfo
{
FileName = url,
UseShellExecute = true
});
}
}
catch (Exception ex)
{
Log.Warning(ex, "Failed to automatically open Swagger UI");
}
});


// ========================================
// Run Application
// ========================================
try
{
    Log.Information("Starting PrintBridge Web API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
