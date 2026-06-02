using AutoMapper;
using Microsoft.Extensions.Logging;
using PrintBridge.Application.DTOs;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Interfaces;

namespace PrintBridge.Application.Services;

public class UserService : IUserService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAccountRepository accountRepository,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountDTO> CreateUserAsync(CreateUserDTO dto)
    {
        // basic validation
        if (string.IsNullOrWhiteSpace(dto.Username)) throw new InvalidOperationException("Username is required");
        if (string.IsNullOrWhiteSpace(dto.Password)) throw new InvalidOperationException("Password is required");

        var existing = await _accountRepository.GetByUsernameAsync(dto.Username);
        if (existing != null) throw new InvalidOperationException("Username already exists");

        var existingEmail = await _accountRepository.GetByEmailAsync(dto.Email);
        if (existingEmail != null) throw new InvalidOperationException("Email already used");

        var hashed = _passwordHasher.HashPassword(dto.Password);

        var account = new Account
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = hashed,
            FullName = dto.FullName?.Trim(),
            IsActive = true,
            Role = dto.IsAdmin ? Domain.Enums.AccountRole.Admin : Domain.Enums.AccountRole.User,
            CreatedAt = DateTime.UtcNow
        };

        await _accountRepository.AddAsync(account);
        _logger.LogInformation("Account created: {Username}", account.Username);
        return _mapper.Map<AccountDTO>(account);
    }

    public async Task<AccountDTO?> GetUserByIdAsync(int id)
    {
        var a = await _accountRepository.GetByIdAsync(id);
        return a == null ? null : _mapper.Map<AccountDTO>(a);
    }

    public async Task<AccountDTO?> GetUserByUsernameAsync(string username)
    {
        var a = await _accountRepository.GetByUsernameAsync(username);
        return a == null ? null : _mapper.Map<AccountDTO>(a);
    }

    public async Task<IEnumerable<AccountDTO>> GetAllUsersAsync()
    {
        var accounts = await _accountRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<AccountDTO>>(accounts);
    }

    public async Task UpdateUserAsync(int id, UpdateUserDTO dto)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null) throw new InvalidOperationException($"User {id} not found");

        if (!string.IsNullOrWhiteSpace(dto.Email)) account.Email = dto.Email.Trim();
        if (dto.FullName != null) account.FullName = dto.FullName.Trim();
        if (dto.IsActive.HasValue) account.IsActive = dto.IsActive.Value;
        if (dto.IsAdmin.HasValue) account.Role = dto.IsAdmin.Value ? Domain.Enums.AccountRole.Admin : Domain.Enums.AccountRole.User;
        account.UpdatedAt = DateTime.UtcNow;

        await _accountRepository.UpdateAsync(account);
    }

    public async Task DeleteUserAsync(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null) throw new InvalidOperationException($"User {id} not found");
        account.IsDeleted = true;
        account.UpdatedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);
    }
}
