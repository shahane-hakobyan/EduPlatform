using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EduPlatform.Api.Auth;

/// <summary>
/// ՄԻԱՅՆ Development-ի համար (տես Program.cs): AuthServer-ի տոկենի փոխարեն թույլ է տալիս թեստավորել
/// header-ներով՝  X-Test-User: &lt;userId&gt;   և (ըստ ցանկության)  X-Test-Role: Teacher|Pupil:
/// Production-ում այս scheme-ը չի գրանցվում:
/// </summary>
public class DevHeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment env) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevHeader";

    // demo սերվերի (Production + DemoAutoLogin) համար թույլատրվում է ՄԻԱՅՆ այս թեստային օգտատերը,
    // որպեսզի header-ով չլինի ներկայանալ ուրիշ օգտատիրոջ անունից
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!env.IsDevelopment() && userId.ToString() != TestUserId)
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Teacher";

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Test " + role),
            new Claim(ClaimTypes.Role, role)
        }, SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
