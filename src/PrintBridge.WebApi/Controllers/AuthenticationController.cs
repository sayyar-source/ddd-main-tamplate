using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using PrintBridge.Application.DTOs;
using PrintBridge.Application.Services;

namespace PrintBridge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authenticationService.LoginAsync(loginRequest);

            if (!response.Success)
            {
                Log.Warning("Failed login attempt for username: {Username}", loginRequest.Username);
                return Unauthorized(response);
            }

            Log.Information("User logged in successfully: {Username}", loginRequest.Username);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in login endpoint");
            return StatusCode(500, new AuthenticationResponseDto
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    [Authorize]
    [HttpPost("validate-token")]
    public async Task<ActionResult<object>> ValidateToken([FromBody] string token)
    {
        try
        {
            var isValid = await _authenticationService.ValidateTokenAsync(token);

            return Ok(new
            {
                valid = isValid,
                message = isValid ? "Token is valid" : "Token is invalid or expired"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating token");
            return StatusCode(500, new { valid = false, message = "Error validating token" });
        }
    }

    [Authorize]
    [HttpGet("me/account")]
    public async Task<ActionResult<AccountDTO>> GetCurrentAccount()
    {
        try
        {
            // Extract token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader)) return Unauthorized();

            var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            
            var account = await _authenticationService.GetCurrentAccount(token);
            if (account == null) return Unauthorized();

            return Ok(account);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current account from token");
            return StatusCode(500, new { message = "Error retrieving account information" });
        }
    }
}