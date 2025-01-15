using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Tests.App.Entry.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    private const string DefaultScheme = "ApiKey";
    public static string Scheme => DefaultScheme;
    public IDictionary<string, string[]> ApiKeyRoles { get; set; } = new Dictionary<string, string[]>();
}

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "x-key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key is missing from the headers"));
        }

        string? providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        // Validate the API key and get associated roles
        if (providedApiKey is null || !Options.ApiKeyRoles.TryGetValue(providedApiKey, out string[]? roles))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided"));
        }

        List<Claim> claims = new() { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsIdentity identity = new(claims, ApiKeyAuthenticationOptions.Scheme);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}