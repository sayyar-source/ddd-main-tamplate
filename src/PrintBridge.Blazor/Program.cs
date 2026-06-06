using PrintBridge.Blazor.Components;
using PrintBridge.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using PrintBridge.Blazor.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddScoped<PrinterApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddHttpClient("ApiClient", op =>
{
    op.BaseAddress = new Uri("http://localhost:5160/");
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication().AddScheme<CustomOption, JWTAuthenticationHandler>("JWTAuth", options => { });
builder.Services.AddScoped<JWTAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider,JWTAuthenticationStateProvider>();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
