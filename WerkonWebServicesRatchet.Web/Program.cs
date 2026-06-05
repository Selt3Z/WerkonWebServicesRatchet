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

builder.Services.AddSingleton<CircuitCookieStore>();
builder.Services.AddScoped<CircuitCookieAccessor>();
builder.Services.AddScoped<ICircuitCookieAccessor>(sp => sp.GetRequiredService<CircuitCookieAccessor>());
builder.Services.AddScoped<AuthCookieContainer>();
builder.Services.AddScoped<ApiSessionCoordinator>();
builder.Services.AddScoped<ApiAuthResponseHandler>();
builder.Services.AddScoped<CircuitHandler, AuthCircuitHandler>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["RatchetApiBaseUrl"] ?? "http://localhost:5277/";

builder.Services.AddHttpClient<RatchetApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .AddHttpMessageHandler<ApiAuthResponseHandler>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var cookieContainer = sp.GetRequiredService<AuthCookieContainer>();
        return new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer.Cookies
        };
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
