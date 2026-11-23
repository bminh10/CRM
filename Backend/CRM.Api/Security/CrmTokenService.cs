using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;

namespace CRM.Api.Security;

public sealed class CrmTokenService(IConfiguration configuration)
{
    private const string Issuer = "CRM.Api";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool IsConfigured => GetKey() is not null;

    public (string AccessToken, string RefreshToken) CreateTokens(Guid userId, string email, string fullName)
    {
        var access = CreateToken(userId, email, fullName, "access", TimeSpan.FromMinutes(15));
        var refresh = CreateToken(userId, email, fullName, "refresh", TimeSpan.FromDays(7));
        return (access, refresh);
    }

    public bool TryValidate(string token, string expectedType, out ClaimsPrincipal? principal)
    {
        principal = null;
        var parts = token.Split('.');
        var key = GetKey();
        if (parts.Length != 3 || key is null)
            return false;

        try
        {
            var input = $"{parts[0]}.{parts[1]}";
            var signature = WebEncoders.Base64UrlDecode(parts[2]);
            var expectedSignature = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(input));
            if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
                return false;

            var header = JsonSerializer.Deserialize<TokenHeader>(WebEncoders.Base64UrlDecode(parts[0]), JsonOptions);
            var payload = JsonSerializer.Deserialize<TokenPayload>(WebEncoders.Base64UrlDecode(parts[1]), JsonOptions);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (header?.Alg != "HS256" || header.Typ != "JWT"
                || payload?.Iss != Issuer || payload.Use != expectedType
                || payload.Exp <= now || !Guid.TryParse(payload.Sub, out var userId))
                return false;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, payload.Email),
                new Claim(ClaimTypes.Name, payload.Name)
            };
            principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
            return true;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    private string CreateToken(Guid userId, string email, string fullName, string use, TimeSpan lifetime)
    {
        var key = GetKey() ?? throw new InvalidOperationException(
            "Authentication signing key is not configured. Set Authentication__SigningKey to at least 32 characters.");
        var header = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new TokenHeader("HS256", "JWT"), JsonOptions));
        var payload = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new TokenPayload(Issuer, userId.ToString(), email, fullName, use,
                DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds()), JsonOptions));
        var input = $"{header}.{payload}";
        var signature = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(input));
        return $"{input}.{WebEncoders.Base64UrlEncode(signature)}";
    }

    private byte[]? GetKey()
    {
        var value = configuration["Authentication:SigningKey"];
        return string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < 32
            ? null
            : Encoding.UTF8.GetBytes(value);
    }

    private sealed record TokenHeader(string Alg, string Typ);
    private sealed record TokenPayload(string Iss, string Sub, string Email, string Name, string Use, long Exp);
}

public sealed class CrmBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    CrmTokenService tokens) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = authorization["Bearer ".Length..].Trim();
        if (!tokens.TryValidate(token, "access", out var principal) || principal is null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired access token."));

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
