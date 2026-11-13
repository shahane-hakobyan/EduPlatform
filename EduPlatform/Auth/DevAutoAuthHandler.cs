using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EduPlatform.Auth;

/// <summary>
/// ՄԻԱՅՆ Development-ի համար (տես Program.cs): բոլոր այցելուներին համարում է մուտք գործած թեստային ուսուցիչ:
/// Production-ում այս scheme-ը չի գրանցվում՝ աշխատում է միայն իրական JWT-ը:
/// </summary>
public class DevAutoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevAuto";

    /// <summary>Էջի վերևի «Ուսուցիչ / Սովորող» փոխարկիչի cookie-ն</summary>
    public const string RoleCookie = "demo_role";

    // նույնն է, ինչ Library.Site-ի և EduPlatform.Api-ի թեստային ուսուցիչը
    public const string TestUserId = "b8a39b93-c8f8-42d2-aba5-5005baf3f1f4";

    // թեստային սովորող
    public const string TestPupilId = "22222222-2222-2222-2222-222222222222";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Դերը՝ էջի վերևի փոխարկիչից (cookie), այլապես appsettings-ի "DemoAutoLoginRole"-ից (լռելյայն՝ Teacher)
        var role = Request.Cookies[RoleCookie] ?? config["DemoAutoLoginRole"];
        var isPupil = string.Equals(role, "Pupil", StringComparison.OrdinalIgnoreCase);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, isPupil ? TestPupilId : TestUserId),
            new Claim(ClaimTypes.Name, isPupil ? "Test Pupil" : "Test Teacher"),
            new Claim(ClaimTypes.Role, isPupil ? "Pupil" : "Teacher")
        }, SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
