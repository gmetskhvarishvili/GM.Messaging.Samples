using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using GM.Messaging.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GM.Messaging.Sample.Common.Admin;

/// <summary>
/// GM.Messaging's admin endpoints (<c>MapGMMessagingAdmin</c>) in every sample host, behind the
/// <see cref="Policy"/> policy. The sample has no identity provider, so the policy accepts an API key in the
/// <see cref="Header"/> header (<c>Admin:ApiKey</c>). A real service would use its own scheme (JWT, mTLS, ...) and
/// pass that policy name instead.
/// </summary>
public static class SampleMessagingAdmin
{
    public const string Policy = "messaging-admin";
    public const string Scheme = "AdminKey";
    public const string Header = "X-Admin-Key";

    /// <summary>The API-key scheme and the <see cref="Policy"/> policy.</summary>
    public static IServiceCollection AddSampleMessagingAdmin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminKeyOptions>(o => o.ApiKey = configuration["Admin:ApiKey"]);
        services.AddAuthentication().AddScheme<AdminKeyOptions, AdminKeyHandler>(Scheme, _ => { });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());
        return services;
    }

    /// <summary>Maps the endpoints under <c>/admin/messaging</c> (outbox, inbox and broker, whichever this host has).</summary>
    public static RouteGroupBuilder MapSampleMessagingAdmin(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGMMessagingAdmin(admin => admin.AuthorizationPolicy = Policy);
}

public sealed class AdminKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>The key callers send; with none configured, nobody gets in.</summary>
    public string? ApiKey { get; set; }
}

/// <summary>Authenticates a request whose <see cref="SampleMessagingAdmin.Header"/> matches <c>Admin:ApiKey</c>.</summary>
public sealed class AdminKeyHandler(IOptionsMonitor<AdminKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AdminKeyOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.ApiKey) || !Request.Headers.TryGetValue(SampleMessagingAdmin.Header, out var sent))
            return Task.FromResult(AuthenticateResult.NoResult());

        var expected = Encoding.UTF8.GetBytes(Options.ApiKey);
        var actual = Encoding.UTF8.GetBytes(sent.ToString());
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return Task.FromResult(AuthenticateResult.Fail("Wrong admin key."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "messaging-admin")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
