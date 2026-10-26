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
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevAuto";

    // նույնն է, ինչ Library.Site-ի և EduPlatform.Api-ի թեստային ուսուցիչը
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, "Test Teacher"),
            new Claim(ClaimTypes.Role, "Teacher")
        }, SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
