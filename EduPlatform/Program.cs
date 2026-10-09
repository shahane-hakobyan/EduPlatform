using EduPlatform.Data;
using EduPlatform.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EduPlatformDbContext>(options =>
options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("EduPlatform.Data")));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IIpGeolocationService, IpGeolocationService>();

builder.Services.AddSignalR();

builder.Services.AddLocalization(options => options.ResourcesPath = "");

builder.Services.AddScoped<ISidebarMenuBuilder, SidebarMenuBuilder>();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    var defaultScheme = builder.Environment.IsDevelopment()
        ? CookieAuthenticationDefaults.AuthenticationScheme
        : JwtBearerDefaults.AuthenticationScheme;

    options.DefaultScheme = defaultScheme;
    options.DefaultChallengeScheme = defaultScheme;
});

if (builder.Environment.IsDevelopment())
{
    authBuilder.AddCookie(options =>
    {
        options.LoginPath = "/Dev/Login";
        options.Cookie.Name = "eduplatform_dev_auth";
    });
}

authBuilder.AddJwtBearer(options =>
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