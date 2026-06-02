using PrintBridge.Application.Mappings;
using PrintBridge.Application.Services;
using PrintBridge.Domain.Interfaces;
using PrintBridge.Infrastructure.Options;
using PrintBridge.Infrastructure.Repositories;
using PrintBridge.Infrastructure.Services;

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
        // Email service uses IOptions<EmailSettings>
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Repositories

        services.AddScoped<IAccountRepository, AccountRepository>();


        // Register user services & repository (add near other services)
        services.AddScoped<IUserService, UserService>();
      
        // AutoMapper - Object-to-object mapping
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());




        return services;
    }
}