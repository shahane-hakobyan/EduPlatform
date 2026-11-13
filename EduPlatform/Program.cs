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

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Views/Shared-ի ենթաթղթապանակները՝ կարճ անուններով գտնելու համար
        options.ViewLocationFormats.Add("/Views/Shared/{1}/{0}.cshtml");   // Views/Shared/<Controller>/
            options.ViewLocationFormats.Add("/Views/Shared/Layouts/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/Partials/{0}.cshtml");
    });

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
builder.Services.AddScoped<IClassRosterService, ClassRosterService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

// «Իմ վիճակագրությունը»՝ գործիքների (IBook, խաչբառ, ...) վիճակագրության API-ներ
builder.Services.AddHttpClient<IMaterialStatsService, MaterialStatsService>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
// ԱԲ զրուցարան. Claude API՝ երբ Anthropic:ApiKey-ը կարգավորված է։
// Բանալի չկա + Development (կամ DemoAutoLogin)՝ ԴԵՄՈ ռեժիմ (պատրաստի պատասխաններ), որ կարողանաք տեսնել և փորձարկել էջը։
var hasClaudeKey = !string.IsNullOrWhiteSpace(builder.Configuration["Anthropic:ApiKey"]);
var allowDemoChat = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("DemoAutoLogin");
if (!hasClaudeKey && allowDemoChat)
    builder.Services.AddSingleton<IClaudeChatService, DemoChatService>();
else
    builder.Services.AddSingleton<IClaudeChatService, ClaudeChatService>();

// ── Մուտք ───────────────────────────────────────────────────────────────────
// Production՝ AuthServer-ի JWT (մուտքի տրամաբանությունը կավարտի մեկ այլ ծրագրավորող):
// Development՝ բոլոր այցելուները համարվում են մուտք գործած թեստային ուսուցիչ:
// Development-ում կամ demo սերվերի "DemoAutoLogin": true դեպքում բոլորը համարվում են թեստային ուսուցիչ:
// Իրական Production-ում այս կարգավորումը չպետք է լինի՝ օգտագործվում է միայն AuthServer-ի JWT-ը:
var autoLogin = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("DemoAutoLogin");

var authBuilder = builder.Services.AddAuthentication(
    autoLogin ? DevAutoAuthHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme);

authBuilder.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.TokenValidationParameters.ValidateAudience = false;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
});

if (autoLogin)
    authBuilder.AddScheme<AuthenticationSchemeOptions, DevAutoAuthHandler>(DevAutoAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

// EF-ի գործիքները (Add-Migration) հավելվածը միայն մոդելը կարդալու համար են գործարկում՝ այդ ժամանակ migration չենք կատարում
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
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
    pattern: "{controller=Home}/{action=Start}/{id?}")
    .WithStaticAssets();

// ԺԱՄԱՆԱԿԱՎՈՐ՝ ստուգելու համար, թե սերվերում ինչ build և ինչ կարգավորում է աշխատում: Ստուգելուց հետո ջնջել:
app.MapGet("/diag", (HttpContext ctx) => Results.Json(new
{
    environment = app.Environment.EnvironmentName,
    demoAutoLogin = builder.Configuration.GetValue<bool>("DemoAutoLogin"),
    autoLoginActive = autoLogin,
    authenticated = ctx.User.Identity?.IsAuthenticated == true,
    scheme = ctx.User.Identity?.AuthenticationType,
    name = ctx.User.Identity?.Name,
    isTeacher = ctx.User.IsInRole("Teacher")
}));
app.Run();
