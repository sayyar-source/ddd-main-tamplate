using Microsoft.AspNetCore.Components.Authorization;
using PrintBridge.Blazor.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PrintBridge.Blazor.Security;

public class JWTAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AccessTokenService accessTokenService;
    public JWTAuthenticationStateProvider(AccessTokenService accessTokenService)
    {
        this.accessTokenService = accessTokenService;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token =await accessTokenService.GetToken();
            if(string.IsNullOrWhiteSpace(token))
            {
              return await MarkAsUnauthorize();
            }
            var readJWT=new JwtSecurityTokenHandler().ReadJwtToken(token);
            var identity=new ClaimsIdentity(readJWT.Claims,"JWT");
            var principal = new ClaimsPrincipal(identity);
            return await Task.FromResult(new AuthenticationState(principal));

        }
        catch (Exception ex)
        {
          return await MarkAsUnauthorize();
        }
    }

    private async Task<AuthenticationState> MarkAsUnauthorize()
    {
        try
        {
            var state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;

        }
        catch (Exception ex)
        {
          return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

    }
}
