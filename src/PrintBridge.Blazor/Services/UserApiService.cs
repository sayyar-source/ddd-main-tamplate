using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PrintBridge.Blazor.DTO.Users;

namespace PrintBridge.Blazor.Services;

public class UserApiService
{
    private readonly HttpClient _http;
    private readonly AccessTokenService _token;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public UserApiService(IHttpClientFactory factory, AccessTokenService token)
    {
        _http = factory.CreateClient("ApiClient");
        _token = token;
    }

    private async Task<HttpRequestMessage> AuthReq(HttpMethod method, string url)
    {
        var t = await _token.GetToken();
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
        return req;
    }

    // GET /api/users
    public async Task<(List<AccountDto>? users, string? error)> GetAllAsync()
    {
        try
        {
            var req = await AuthReq(HttpMethod.Get, "api/users");
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<List<AccountDto>>(_json), null);
            return (null, $"HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // POST /api/users
    public async Task<(AccountDto? user, string? error)> CreateAsync(CreateUserRequest dto)
    {
        try
        {
            var req = await AuthReq(HttpMethod.Post, "api/users");
            req.Content = JsonContent.Create(dto);
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<AccountDto>(_json), null);
            var body = await res.Content.ReadAsStringAsync();
            return (null, $"HTTP {(int)res.StatusCode}: {body}");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // PUT /api/users/{id}
    public async Task<string?> UpdateAsync(int id, UpdateUserRequest dto)
    {
        try
        {
            var req = await AuthReq(HttpMethod.Put, $"api/users/{id}");
            req.Content = JsonContent.Create(dto);
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode) return null;
            var body = await res.Content.ReadAsStringAsync();
            return $"HTTP {(int)res.StatusCode}: {body}";
        }
        catch (Exception ex) { return ex.Message; }
    }

    // DELETE /api/users/{id}
    public async Task<string?> DeleteAsync(int id)
    {
        try
        {
            var req = await AuthReq(HttpMethod.Delete, $"api/users/{id}");
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode) return null;
            return $"HTTP {(int)res.StatusCode}";
        }
        catch (Exception ex) { return ex.Message; }
    }

    // GET /api/authentication/me/account
    public async Task<(AccountDto? account, string? error)> GetCurrentAccountAsync()
    {
        try
        {
            var req = await AuthReq(HttpMethod.Get, "api/authentication/me/account");
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<AccountDto>(_json), null);
            return (null, $"HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // GET /health
    public async Task<(string status, bool healthy)> GetHealthAsync()
    {
        try
        {
            var res = await _http.GetAsync("health");
            return res.IsSuccessStatusCode ? ("healthy", true) : ("degraded", false);
        }
        catch { return ("unreachable", false); }
    }
}
