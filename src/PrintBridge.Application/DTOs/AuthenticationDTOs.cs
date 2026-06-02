namespace PrintBridge.Application.DTOs;
public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public class AuthenticationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public AccountDTO? AcountDTO { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
