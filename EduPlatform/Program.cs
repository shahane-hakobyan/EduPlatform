using Microsoft.AspNetCore.Authentication.JwtBearer;

using Hangfire;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using EduPlatform.Data;
using EduPlatform.Services;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllersWithViews();

// EF Core
builder.Services.AddDbContext<EduPlatformDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") + ",abortConnect=false"));

// IP geolocation (used by the sidebar flag)
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIpGeolocationService, IpGeolocationService>();

// SignalR + Redis backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!);

// Hangfire
builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "");

// Sidebar menu builder (localized)
builder.Services.AddScoped<ISidebarMenuBuilder, SidebarMenuBuilder>();

// EduPlatform sits behind the Gateway (YARP), which owns login/session and
// forwards the user's OpenIddict-issued access token as a Bearer header on
// every proxied request. EduPlatform validates that token itself, the same
// way the other downstream services (ocr, ibook, crossword, learn, ...) do.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        // Generic token shared across all downstream services for now, so no
        // audience is enforced. Revisit if EduPlatform gets its own OpenIddict
        // resource/audience later.
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
var supportedCultures = new[] { "hy", "ru", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("hy")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=MainPage}/{id?}")
    .WithStaticAssets();

app.Run();