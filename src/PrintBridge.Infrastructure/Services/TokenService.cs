using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using PrintBridge.Application.DTOs;
using PrintBridge.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PrintBridge.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "PrintBridgeAPI";
        _audience = configuration["Jwt:Audience"] ?? "PrintBridgeClient";
        _expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var minutes)? minutes: 60;
    }

    public string GenerateToken(AccountDTO acountDTO)
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, acountDTO.Id.ToString()),
                new Claim(ClaimTypes.Name, acountDTO.Username),
                new Claim(ClaimTypes.Email, acountDTO.Email),
                new Claim("FullName", acountDTO.FullName ?? string.Empty),
                new Claim("IsActive", acountDTO.IsActive.ToString()),
                new Claim(ClaimTypes.Role, acountDTO.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddDays(90),
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            Log.Information("JWT token generated for supplier {SupplierId}: {Username}",
                acountDTO.Id, acountDTO.Username);

            return tokenString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating JWT token for supplier {SupplierId}", acountDTO.Id);
            throw;
        }
    }
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
            }, out SecurityToken validatedToken);

            return validatedToken is JwtSecurityToken;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Token validation failed");
            return false;
        }
    }

    public int GetAccountIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            var supplierId = jwtToken?.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            return int.Parse(supplierId ?? "0");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error extracting supplier ID from token");
            return 0;
        }
    }
    public string GetUsernameFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error extracting username from token");
            return string.Empty;
        }
    }
}