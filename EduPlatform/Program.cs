using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using EduPlatform.Data;
using EduPlatform.Services;
using Serilog;

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

// In-memory cache (replaces Redis for the sidebar geolocation lookup)
builder.Services.AddMemoryCache();

// IP geolocation (used by the sidebar flag)
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIpGeolocationService, IpGeolocationService>();

// SignalR (no Redis backplane - fine for a single server)
builder.Services.AddSignalR();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "");

// Sidebar menu builder (localized)
builder.Services.AddScoped<ISidebarMenuBuilder, SidebarMenuBuilder>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EduPlatformDbContext>();
    db.Database.Migrate();
}

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
// Only honor an explicit language pick (cookie/querystring) — otherwise always default to Armenian,
// instead of falling back to the browser's Accept-Language header.
localizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
{
    new CookieRequestCultureProvider(),
};
app.UseRequestLocalization(localizationOptions);

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=MainPage}/{id?}")
    .WithStaticAssets();

app.Run();