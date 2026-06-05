using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Localization;
using WerkonWebServicesRatchet.Web.Components;
using WerkonWebServicesRatchet.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var supportedCultures = new[] { "ru-RU", "en-US", "ja-JP" };

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("ru-RU");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider()
    ];
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CircuitCookieStore>();
builder.Services.AddScoped<BrowserSessionCookie>();
builder.Services.AddScoped<CircuitCookieAccessor>();
builder.Services.AddScoped<ICircuitCookieAccessor>(sp => sp.GetRequiredService<CircuitCookieAccessor>());
builder.Services.AddScoped<AuthCookieContainer>();
builder.Services.AddScoped<ApiSessionCoordinator>();
builder.Services.AddScoped<CircuitHandler, AuthCircuitHandler>();
builder.Services.AddSingleton<AppTimeZone>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["RatchetApiBaseUrl"] ?? "http://localhost:5277/";

builder.Services.AddScoped<RatchetApiClient>(sp =>
{
    var cookieContainer = sp.GetRequiredService<AuthCookieContainer>();
    cookieContainer.RestoreFromStore();

    var sessionCoordinator = sp.GetRequiredService<ApiSessionCoordinator>();
    var handler = new ApiAuthResponseHandler(sessionCoordinator, cookieContainer)
    {
        InnerHandler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer.Cookies,
            UseProxy = ShouldUseSystemProxy(apiBaseUrl)
        }
    };

    var httpClient = new HttpClient(handler, disposeHandler: true)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };

    return new RatchetApiClient(httpClient, sp.GetRequiredService<AppTimeZone>());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLocalization();

app.MapGet("/culture/set/{culture}", (string culture, string? returnUrl, HttpContext httpContext) =>
{
    var selectedCulture = LocalizationService.NormalizeCultureName(culture) ?? "ru-RU";

    httpContext.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

    var redirectUrl = LocalizationService.GetSafeReturnUrl(returnUrl);
    return Results.LocalRedirect(redirectUrl);
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool ShouldUseSystemProxy(string apiBaseUrl)
{
    if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return uri.Host is not ("localhost" or "127.0.0.1" or "[::1]");
}
