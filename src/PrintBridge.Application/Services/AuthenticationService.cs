using AutoMapper;
using Microsoft.Extensions.Logging;
using PrintBridge.Application.DTOs;
using PrintBridge.Domain.Enums;
using PrintBridge.Domain.Interfaces;

namespace PrintBridge.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IAccountRepository accountRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        ILogger<AuthenticationService> logger)
    {
        _accountRepository= accountRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AccountDTO> GetCurrentAccount(string token)
    {
        bool isValid = _tokenService.ValidateToken(token);
        if (!isValid)
        {
            _logger.LogWarning("Invalid token provided for GetCurrentAccount");
            return null;
        }
        var accountId = _tokenService.GetAccountIdFromToken(token);
        var account = await _accountRepository.GetByIdAsync(accountId);
        return _mapper.Map<AccountDTO>(account);
    }

    public async Task<AuthenticationResponseDto> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            // Single auth source: Accounts
            var account = await _accountRepository.GetByUsernameAsync(loginRequest.Username);
            if (account == null)
            {
                _logger.LogWarning("Login attempt with non-existent username: {Username}", loginRequest.Username);
                return new AuthenticationResponseDto { Success = false, Message = "Invalid username or password" };
            }

            if (!account.IsActive)
            {
                _logger.LogWarning("Login attempt with inactive account: {Username}", loginRequest.Username);
                return new AuthenticationResponseDto { Success = false, Message = "Account is inactive" };
            }

            if (!_passwordHasher.VerifyPassword(loginRequest.Password, account.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for account: {Username}", loginRequest.Username);
                return new AuthenticationResponseDto { Success = false, Message = "Invalid username or password" };
            }

            // Map account DTO
            var accountDto = _mapper.Map<AccountDTO>(account);

            // Generate token (includes role claim in TokenService)
            var token = _tokenService.GenerateToken(accountDto);

            var response = new AuthenticationResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                AcountDTO = accountDto,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            _logger.LogInformation("Account logged in: {Username} (Id: {Id}, Role: {Role})", account.Username, account.Id, account.Role);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", loginRequest.Username);
            return new AuthenticationResponseDto { Success = false, Message = "An error occurred during login" };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        return await Task.FromResult(_tokenService.ValidateToken(token));
    }

}