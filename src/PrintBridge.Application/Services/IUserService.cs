using PrintBridge.Application.DTOs;

namespace PrintBridge.Application.Services;

public interface IUserService
{
    Task<AccountDTO> CreateUserAsync(CreateUserDTO dto);
    Task<AccountDTO?> GetUserByIdAsync(int id);
    Task<AccountDTO?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<AccountDTO>> GetAllUsersAsync();
    Task UpdateUserAsync(int id, UpdateUserDTO dto);
    Task DeleteUserAsync(int id); // soft delete
}
