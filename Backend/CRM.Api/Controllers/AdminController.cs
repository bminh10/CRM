using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("system-info")]
    public IActionResult SystemInfo() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "System-wide admin authorization is unavailable because the current schema has no global administrator role."
    });

    [HttpGet("metrics")]
    public IActionResult Metrics() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Metrics require an administrator authorization policy and an approved metrics provider."
    });

    [HttpPost("backup")]
    public IActionResult CreateBackup() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Backups are not configured. Configure an isolated backup service, storage target and administrator approval first."
    });

    [HttpGet("backups")]
    public IActionResult GetBackups() => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Backups are not configured."
    });

    [HttpPost("backups/{id:guid}/restore")]
    public IActionResult RestoreBackup(Guid id) => StatusCode(StatusCodes.Status501NotImplemented, new
    {
        message = "Restore is not configured; no backup storage or restore approval workflow is available."
    });
}
