using PrintBridge.Domain.Entities.Base;
using PrintBridge.Domain.Enums;

namespace PrintBridge.Domain.Entities;

public class Account : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public AccountRole Role { get; set; }


}