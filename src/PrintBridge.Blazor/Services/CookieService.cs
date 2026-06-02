using Microsoft.JSInterop;

namespace PrintBridge.Blazor.Services;

public class CookieService
{
    private readonly IJSRuntime jSRuntime;
    public CookieService(IJSRuntime jSRuntime)
    {
        this.jSRuntime = jSRuntime;
    }

    public async Task<string> Get(string key)
    {
        string result = await jSRuntime.InvokeAsync<string>("getCookie",key);
        return result;
    }

    public async Task Remove(string key)
    {
        await jSRuntime.InvokeVoidAsync("deleteCookie",key);
    }

    public async Task Set(string key, string value, int days) 
    {
        await jSRuntime.InvokeVoidAsync("setCookie",key,value,days);
    }
}
