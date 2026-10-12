using HTHIUM.Mes.Api.BackgroundServices;
using HTHIUM.Mes.Api.Realtime;
using HTHIUM.Mes.Api.Security;
using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Infrastructure.Database;
using HTHIUM.Mes.Infrastructure.Devices;
using HTHIUM.Mes.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "hthium_mes_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
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
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebDevelopment", policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var monitorOptions = builder.Configuration
    .GetSection("DeviceMonitoring")
    .Get<DeviceMonitoringOptions>() ?? new DeviceMonitoringOptions();
builder.Services.AddSingleton(monitorOptions);
var databaseConnectionString = builder.Configuration.GetConnectionString("MesDatabase")
    ?? throw new InvalidOperationException("缺少ConnectionStrings:MesDatabase配置");
builder.Services.AddSingleton(new MesDatabaseOptions
{
    ConnectionString = databaseConnectionString,
});
builder.Services.Configure<DeviceAccessOptions>(
    builder.Configuration.GetSection("DeviceAccess"));
builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection("BootstrapAdmin"));
builder.Services.AddSingleton<IMesDatabaseInitializer, MesDatabaseInitializer>();
builder.Services.AddSingleton<IDeviceStateRepository, MySqlDeviceStateRepository>();
builder.Services.AddSingleton<IDeviceRealtimeNotifier, SignalRDeviceNotifier>();
builder.Services.AddSingleton<IDeviceMonitorService, DeviceMonitorService>();
builder.Services.AddSingleton<IUserRepository, MySqlUserRepository>();
builder.Services.AddSingleton<IUserPasswordHasher, AspNetUserPasswordHasher>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<BootstrapAdminService>();
builder.Services.AddHostedService<DeviceOfflineMonitorService>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("DatabaseInitialization:ApplySchema"))
{
    await app.Services.GetRequiredService<IMesDatabaseInitializer>()
        .InitializeAsync(app.Lifetime.ApplicationStopping);
}

await app.Services.GetRequiredService<BootstrapAdminService>()
    .InitializeAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseCors("WebDevelopment");
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<DeviceApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();
app.MapHub<OperationsHub>("/hubs/operations");
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
