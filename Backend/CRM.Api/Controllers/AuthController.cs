using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using CRM.Api.Data;
using CRM.Api.Models;
using CRM.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(CrmDbContext db, CrmTokenService tokens, IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [HttpPost("session/register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.FullName) || request.Email.Length > 255
            || request.Password.Length is < 12 or > 1024 || request.FullName.Trim().Length > 150
            || !IsValidEmail(request.Email))
            return BadRequest(new { message = "A valid email, full name up to 150 characters and password of 12 to 1024 characters are required." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (!tokens.IsConfigured)
            return StatusCode(503, new { message = "Authentication signing key is not configured." });
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = request.FullName.Trim(),
            Password = HashPassword(request.Password),
            IsVerified = false,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        if (IsCookieSessionRequest())
            return Created($"/api/users/{user.Id}", CreateCookieSession(user));
        return Created($"/api/users/{user.Id}", new
        {
            user = new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl),
            tokens = CreateTokenResponse(user)
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [HttpPost("session/login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
            || request.Email.Length > 255 || request.Password.Length > 1024)
            return BadRequest(new { message = "Email and password are required." });
        if (!tokens.IsConfigured)
            return StatusCode(503, new { message = "Authentication signing key is not configured." });
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.Password))
            return Unauthorized(new { message = "Email or password is incorrect." });

        if (!IsHashedPassword(user.Password))
        {
            user.Password = HashPassword(request.Password);
            user.UpdateAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (IsCookieSessionRequest())
            return Ok(CreateCookieSession(user));
        return Ok(new { user = new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl), tokens = CreateTokenResponse(user) });
    }

    [AllowAnonymous]
    [HttpPost("session/logout")]
    public IActionResult SessionLogout()
    {
        CrmSessionCookies.Clear(HttpContext, environment);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("session/refresh")]
    public async Task<IActionResult> RefreshSession(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Request.Cookies.TryGetValue(CrmSessionCookies.RefreshName, out var refreshToken)
            || !tokens.TryValidate(refreshToken, "refresh", out var principal)
            || !Guid.TryParse(principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized(new { message = "Account no longer exists." });
        return Ok(CreateCookieSession(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new
    {
        message = "This API uses stateless tokens. Remove tokens on the client; existing tokens remain valid until expiry."
    });

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "RefreshToken is required." });
        if (!tokens.TryValidate(request.RefreshToken, "refresh", out var principal)
            || !Guid.TryParse(principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized(new { message = "Account no longer exists." });
        return Ok(new { user = new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl), tokens = CreateTokenResponse(user) });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Password reset requires a configured email sender and persistent, single-use reset token storage."
    });

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public IActionResult ResetPassword() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Password reset requires persistent, single-use reset token storage."
    });

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl));
    }

    private object CreateTokenResponse(User user)
    {
        var result = tokens.CreateTokens(user.Id, user.Email, user.FullName);
        return new { accessToken = result.AccessToken, refreshToken = result.RefreshToken, tokenType = "Bearer", expiresIn = 900 };
    }

    private bool IsCookieSessionRequest() => Request.Path.StartsWithSegments("/api/auth/session");

    private object CreateCookieSession(User user)
    {
        var result = tokens.CreateTokens(user.Id, user.Email, user.FullName);
        CrmSessionCookies.Set(HttpContext, environment, result.AccessToken, result.RefreshToken);
        return new { user = new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl) };
    }

    internal static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 310_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    internal static bool VerifyPassword(string password, string stored)
    {
        if (!IsHashedPassword(stored))
            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(password)),
                SHA256.HashData(Encoding.UTF8.GetBytes(stored)));
        var parts = stored.Split('$');
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 310_000, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsHashedPassword(string password) => password.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal);

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record RegisterRequest(string Email, string Password, string FullName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record UserResponse(Guid Id, string Email, string FullName, string? AvatarUrl);
