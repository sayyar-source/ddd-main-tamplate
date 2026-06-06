namespace PrintBridge.Blazor.DTO.Users;

public class AccountDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public int Role { get; set; } // 1=Admin, 2=User, 3=Supplier

    public string RoleLabel => Role switch
    {
        1 => "Admin",
        2 => "User",
        3 => "Supplier",
        _ => "Unknown"
    };
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
}
