using PrintBridge.Application.Mappings;
using PrintBridge.Application.Services;
using PrintBridge.Domain.Interfaces;
using PrintBridge.Infrastructure.Options;
using PrintBridge.Infrastructure.Repositories;
using PrintBridge.Infrastructure.Services;
using PrintBridge.Infrastructure.Services.Printer;

namespace PrintBridge.WebApi.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Application Services Layer
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Infrastructure Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();

        // User services
        services.AddScoped<IUserService, UserService>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        // ── Printer Services ─────────────────────────────────────────────────
        var logDir = configuration["Printer:LogDirectory"] ?? "logs";
        services.AddSingleton(new PrintLogRepository(logDir));
        services.AddSingleton<UsbPrinterConnection>();
        services.AddSingleton<LanPrinterConnection>();
        services.AddSingleton<PrinterManager>();
        services.AddHostedService(sp => sp.GetRequiredService<PrinterManager>());

        return services;
    }
}