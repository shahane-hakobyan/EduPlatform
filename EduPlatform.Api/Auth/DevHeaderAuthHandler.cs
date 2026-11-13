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

    // demo սերվերի (Production + DemoAutoLogin) համար թույլատրվում են ՄԻԱՅՆ այս թեստային օգտատերերը,
    // որպեսզի header-ով չլինի ներկայանալ ուրիշ օգտատիրոջ անունից
    public const string TestUserId = "b8a39b93-c8f8-42d2-aba5-5005baf3f1f4";   // ուսուցիչ
    public const string TestPupilId = "22222222-2222-2222-2222-222222222222";  // սովորող

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var id = userId.ToString();
        if (!env.IsDevelopment() && id != TestUserId && id != TestPupilId)
            return Task.FromResult(AuthenticateResult.NoResult());

        // demo սերվերում դերը որոշվում է id-ով (header-ով չի կարելի ուսուցչին սովորող դարձնել և հակառակը)
        var role = env.IsDevelopment()
            ? Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Teacher"
            : (id == TestPupilId ? "Pupil" : "Teacher");

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
