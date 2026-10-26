using EduPlatform.Api.Auth;
using EduPlatform.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<EduPlatformDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("EduPlatform.Data")));

// Նույն JWT վավերացումը, ինչ EduPlatform կայքում (AuthServer-ի տոկեն)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

// ՄԻԱՅՆ Development: թեստավորում առանց AuthServer-ի՝ X-Test-User / X-Test-Role header-ներով.
// Եթե հարցումը X-Test-User header ունի՝ օգտագործվում է DevHeader-ը, այլապես՝ սովորական JWT-ը:
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = "DevOrJwt";
        o.DefaultAuthenticateScheme = "DevOrJwt";
        o.DefaultChallengeScheme = "DevOrJwt";
    })
        .AddPolicyScheme("DevOrJwt", "Dev header or JWT", o =>
            o.ForwardDefaultSelector = ctx =>
                ctx.Request.Headers.ContainsKey("X-Test-User")
                    ? DevHeaderAuthHandler.SchemeName
                    : JwtBearerDefaults.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, DevHeaderAuthHandler>(DevHeaderAuthHandler.SchemeName, _ => { });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
