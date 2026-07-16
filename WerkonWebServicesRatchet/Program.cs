using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Infrastructure.Audit;
using WerkonWebServicesRatchet.Infrastructure.Authorization;
using WerkonWebServicesRatchet.Infrastructure.Backups;
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "Set the ConnectionStrings__DefaultConnection environment variable (see .env.example).");

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString)
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

    options.AddPolicy(AuthorizationPolicies.CreateCatalogService, policy =>
        policy.Requirements.Add(new CreateCatalogServiceRequirement()));

    options.AddPolicy(AuthorizationPolicies.AssignVisitMechanic, policy =>
        policy.RequireRole(AppRoles.CanAssignVisitMechanic));

    options.AddPolicy(AuthorizationPolicies.ViewAuditLog, policy =>
        policy.RequireRole(AppRoles.CanViewAuditLog));

    options.AddPolicy(AuthorizationPolicies.HardDeleteRecords, policy =>
        policy.Requirements.Add(new HardDeleteRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, HardDeleteAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CreateCatalogServiceAuthorizationHandler>();
builder.Services.AddScoped<BackupStatusReader>();
builder.Services.AddSingleton<BackupCatalogService>();
builder.Services.AddSingleton<NetworkInfoReader>();
builder.Services.AddScoped<DatabaseRestoreService>();
builder.Services.AddHostedService<AuditRetentionHostedService>();

builder.Services.AddSingleton<AppTimeZone>();
builder.Services.AddSingleton<VisitWorkOrderPdfGenerator>();
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddHostedService<IdentitySeedHostedService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();

public partial class Program;
