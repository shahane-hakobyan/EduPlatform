using EduPlatform.Auth;
using EduPlatform.Data;
using EduPlatform.Services;
using Microsoft.AspNetCore.Authentication;
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

builder.Services.AddHttpClient<ILibraryApiClient, LibraryApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["LibraryApi:BaseUrl"]!.TrimEnd('/') + "/");

    // Library.Api-ն Production-ում ընդունում է միայն բանալիով հարցումներ
    var apiKey = builder.Configuration["LibraryApi:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        c.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

builder.Services.AddScoped<IIpGeolocationService, IpGeolocationService>();

builder.Services.AddSignalR();

builder.Services.AddLocalization(options => options.ResourcesPath = "");

builder.Services.AddScoped<ISidebarMenuBuilder, SidebarMenuBuilder>();

// ── Մուտք ───────────────────────────────────────────────────────────────────
// Production՝ AuthServer-ի JWT (մուտքի տրամաբանությունը կավարտի մեկ այլ ծրագրավորող):
// Development՝ բոլոր այցելուները համարվում են մուտք գործած թեստային ուսուցիչ:
var authBuilder = builder.Services.AddAuthentication(
    builder.Environment.IsDevelopment() ? DevAutoAuthHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme);

authBuilder.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.TokenValidationParameters.ValidateAudience = false;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
});

if (builder.Environment.IsDevelopment())
    authBuilder.AddScheme<AuthenticationSchemeOptions, DevAutoAuthHandler>(DevAutoAuthHandler.SchemeName, _ => { });

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
