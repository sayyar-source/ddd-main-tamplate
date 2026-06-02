namespace PrintBridge.Application.DTOs;

public class UpdateUserDTO
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
}
