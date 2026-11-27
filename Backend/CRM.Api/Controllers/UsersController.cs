using System.Security.Claims;
using CRM.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetUsers() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Global user listing requires a system administrator role, which is not represented in the current schema."
    });

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(new UserProfileResponse(user.Email, user.FullName, user.OtpSecret is not null));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        if (id != currentUserId)
            return Forbid();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();
        if (id != currentUserId)
            return Forbid();
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "FullName is required." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return NotFound();
        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl));
    }

    [HttpPatch("{id:guid}/status")]
    public IActionResult UpdateStatus(Guid id) => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Account status changes require an account status field and system administrator authorization."
    });

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Account deletion requires an explicit data-retention and workspace ownership transfer policy."
    });

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record UpdateUserRequest(string FullName, string? AvatarUrl);
public sealed record UserProfileResponse(string Email, string FullName, bool IsOtpConfigured);
