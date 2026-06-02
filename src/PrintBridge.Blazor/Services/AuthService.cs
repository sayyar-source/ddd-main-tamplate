using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using PrintBridge.Blazor.DTO;

namespace PrintBridge.Blazor.Services;

public class AuthService
{
    private readonly AccessTokenService accessTokenService;
    private readonly NavigationManager navigationManager;
    private HttpClient client;
    public AuthService(AccessTokenService accessTokenService,NavigationManager navigationManager,IHttpClientFactory httpClientFactory)
    {
        this.accessTokenService = accessTokenService;
        this.navigationManager = navigationManager;
        client= httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<bool> LoginAsync(string username, string password) 
    {
        var status = await client.PostAsJsonAsync("api/Authentication/login", new { username, password });
        if (status.IsSuccessStatusCode)
        {
            var token=await status.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthResponse>(token);
            await accessTokenService.SetToken(result!.Token!);
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await accessTokenService.RemoveToken();
        navigationManager.NavigateTo("/login");
    }
}
