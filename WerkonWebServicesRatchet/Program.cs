using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Infrastructure.Audit;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Pdf;
using WerkonWebServicesRatchet.Infrastructure.Settings;
using WerkonWebServicesRatchet.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditEntryFactory>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Ratchet.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.BusinessData, policy =>
        policy.RequireRole(AppRoles.BusinessUsers));

    options.AddPolicy(AuthorizationPolicies.DeleteServiceItems, policy =>
        policy.RequireRole(AppRoles.CanDeleteServiceItems));

    options.AddPolicy(AuthorizationPolicies.ManageUsers, policy =>
        policy.RequireRole(AppRoles.CanManageUsers));

    options.AddPolicy(AuthorizationPolicies.ManageServiceCatalog, policy =>
        policy.RequireRole(AppRoles.CanManageServiceCatalog));

    options.AddPolicy(AuthorizationPolicies.AssignVisitMechanic, policy =>
        policy.RequireRole(AppRoles.CanAssignVisitMechanic));
});

builder.Services.AddSingleton<AppTimeZone>();
builder.Services.AddSingleton<VisitWorkOrderPdfGenerator>();
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddHostedService<IdentitySeedHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
