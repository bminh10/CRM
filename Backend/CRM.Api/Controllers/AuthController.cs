using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using CRM.Api.Data;
using CRM.Api.Models;
using CRM.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    CrmDbContext db,
    CrmTokenService tokens,
    IWebHostEnvironment environment,
    IDataProtectionProvider protectionProvider) : ControllerBase
{
    private readonly IDataProtector otpProtector = protectionProvider.CreateProtector("CRM.Api.UserOtp.v1");

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
    [EnableRateLimiting("otp")]
    [HttpPost("otp/setup")]
    public async Task<IActionResult> SetupOtp(SetupOtpRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();
        if (!EmailMatches(request.Email, user.Email))
            return BadRequest(new { message = "Email không khớp với tài khoản hiện tại." });
        if (user.OtpSecret is not null)
            return Conflict(new { message = "OTP đã được thiết lập cho tài khoản này." });

        var secret = TotpService.GenerateSecret();
        user.OtpPendingSecret = otpProtector.Protect(secret);
        user.OtpPendingExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new SetupOtpResponse(secret, TotpService.CreateProvisioningUri(user.Email, secret), user.OtpPendingExpiresAt.Value));
    }

    [Authorize]
    [EnableRateLimiting("otp")]
    [HttpPost("otp/verify-setup")]
    public async Task<IActionResult> VerifyOtpSetup(VerifyOtpSetupRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();
        if (!EmailMatches(request.Email, user.Email))
            return BadRequest(new { message = "Email không khớp với tài khoản hiện tại." });
        if (user.OtpSecret is not null)
            return Conflict(new { message = "OTP đã được thiết lập cho tài khoản này." });
        if (user.OtpPendingSecret is null || user.OtpPendingExpiresAt <= DateTime.UtcNow)
        {
            user.OtpPendingSecret = null;
            user.OtpPendingExpiresAt = null;
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = "Phiên thiết lập OTP đã hết hạn. Hãy bắt đầu thiết lập lại." });
        }

        string secret;
        try
        {
            secret = otpProtector.Unprotect(user.OtpPendingSecret);
        }
        catch (CryptographicException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Không thể đọc cấu hình OTP. Vui lòng liên hệ quản trị viên." });
        }
        if (!TotpService.TryVerify(secret, request.Code, DateTimeOffset.UtcNow, out var matchedStep))
            return BadRequest(new { message = "Mã OTP không hợp lệ." });

        var now = DateTime.UtcNow;
        var updated = await db.Users
            .Where(x => x.Id == userId && x.OtpSecret == null
                && x.OtpPendingSecret == user.OtpPendingSecret
                && x.OtpPendingExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.OtpSecret, user.OtpPendingSecret)
                .SetProperty(x => x.OtpPendingSecret, (string?)null)
                .SetProperty(x => x.OtpPendingExpiresAt, (DateTime?)null)
                .SetProperty(x => x.OtpLastUsedStep, (long?)matchedStep)
                .SetProperty(x => x.UpdateAt, now), cancellationToken);
        if (updated == 0)
            return Conflict(new { message = "Phiên thiết lập OTP đã hết hạn hoặc được xác nhận trước đó." });
        return Ok(new { isOtpConfigured = true });
    }

    [Authorize]
    [EnableRateLimiting("otp")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length is < 12 or > 1024)
            return BadRequest(new { message = "Mật khẩu mới phải có từ 12 đến 1024 ký tự." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();
        if (!EmailMatches(request.Email, user.Email))
            return BadRequest(new { message = "Email không khớp với tài khoản hiện tại." });
        if (user.OtpSecret is null)
            return Conflict(new { message = "Hãy thiết lập OTP trong Hồ sơ trước khi đổi mật khẩu." });

        string secret;
        try
        {
            secret = otpProtector.Unprotect(user.OtpSecret);
        }
        catch (CryptographicException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Không thể đọc cấu hình OTP. Vui lòng liên hệ quản trị viên." });
        }
        if (!TotpService.TryVerify(secret, request.Code, DateTimeOffset.UtcNow, out var matchedStep)
            || matchedStep <= (user.OtpLastUsedStep ?? -1))
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã được sử dụng." });

        var now = DateTime.UtcNow;
        var updated = await db.Users
            .Where(x => x.Id == userId && x.OtpSecret == user.OtpSecret
                && (x.OtpLastUsedStep == null || x.OtpLastUsedStep < matchedStep))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Password, HashPassword(request.NewPassword))
                .SetProperty(x => x.OtpLastUsedStep, (long?)matchedStep)
                .SetProperty(x => x.UpdateAt, now), cancellationToken);
        if (updated == 0)
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã được sử dụng." });
        return Ok(new { message = "Đã đổi mật khẩu." });
    }

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

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out userId);

    private static bool EmailMatches(string submittedEmail, string accountEmail) =>
        !string.IsNullOrWhiteSpace(submittedEmail)
        && string.Equals(submittedEmail.Trim(), accountEmail, StringComparison.OrdinalIgnoreCase);

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
public sealed record SetupOtpRequest(string Email);
public sealed record SetupOtpResponse(string Secret, string ProvisioningUri, DateTime ExpiresAt);
public sealed record VerifyOtpSetupRequest(string Email, string Code);
public sealed record ChangePasswordRequest(string Email, string Code, string NewPassword);
public sealed record UserResponse(Guid Id, string Email, string FullName, string? AvatarUrl);
