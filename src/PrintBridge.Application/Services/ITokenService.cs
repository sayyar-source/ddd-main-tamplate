using PrintBridge.Application.DTOs;
namespace PrintBridge.Application.Services;
public interface ITokenService
{
    string GenerateToken(AccountDTO acountDTO);
    bool ValidateToken(string token);
    int GetAccountIdFromToken(string token);
    string GetUsernameFromToken(string token);
}