namespace PrintBridge.Application.DTOs;

public class CreateUserDTO
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsAdmin { get; set; } = false;
}