using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PrintBridge.Blazor.Security;

public class JWTAuthenticationHandler : AuthenticationHandler<CustomOption>
{
    public JWTAuthenticationHandler(IOptionsMonitor<CustomOption> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
		try
		{
			var token = Request.Cookies["access_token"];
			if (string.IsNullOrEmpty(token))
				return AuthenticateResult.NoResult();

            var readJWT = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var identity = new ClaimsIdentity(readJWT.Claims, "JWT");
            var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
		catch (Exception ex)
		{
            return AuthenticateResult.NoResult();
        }
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/login");
        return Task.CompletedTask;
    }
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/accessdedied");
        return Task.CompletedTask;
    }
}
public class CustomOption:AuthenticationSchemeOptions
{

}