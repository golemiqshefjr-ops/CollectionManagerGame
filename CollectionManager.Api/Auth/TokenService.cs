using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CollectionManager.Api.Auth;

public sealed class TokenService(IConfiguration configuration)
{
    private readonly string _key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing JWT signing key.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "CollectionManager";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "CollectionManager.Web";

    public AuthResponse Create(int id, string email, string role, string name)
    {
        var expires = DateTime.UtcNow.AddHours(8);
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { sub = id.ToString(), email, role, name, iss = _issuer, aud = _audience, exp = new DateTimeOffset(expires).ToUnixTimeSeconds() }));
        var unsigned = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_key));
        return new AuthResponse($"{unsigned}.{Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)))}", role, name, expires);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var unsigned = $"{parts[0]}.{parts[1]}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_key));
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(parts[2]))) return null;
            using var payload = JsonDocument.Parse(FromBase64Url(parts[1]));
            var root = payload.RootElement;
            if (root.GetProperty("iss").GetString() != _issuer || root.GetProperty("aud").GetString() != _audience || root.GetProperty("exp").GetInt64() <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, root.GetProperty("sub").GetString()!),
                new Claim(ClaimTypes.Name, root.GetProperty("name").GetString()!),
                new Claim(ClaimTypes.Email, root.GetProperty("email").GetString()!),
                new Claim(ClaimTypes.Role, root.GetProperty("role").GetString()!)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }
        catch { return null; }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string text) => Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/') + new string('=', (4 - text.Length % 4) % 4));
}

public sealed class JwtAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, TokenService tokens)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var value = Request.Headers.Authorization.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(AuthenticateResult.NoResult());
        var principal = tokens.Validate(value[7..].Trim());
        if (principal is null) return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token."));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

