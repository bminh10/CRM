
using CRM.Api.Business.Interface;
using CRM.Api.Data;
using CRM.Api.DTOs.Companies;
using CRM.Api.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly CrmDbContext _db;

    public CompaniesController(ICompanyService companyService, CrmDbContext db)
    {
        _companyService = companyService;
        _db = db;
    }

    // GET: api/companies?workspaceId={id}
    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetCompanies(
        [FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "WorkspaceId is required."
            });
        }
        if (!await IsMember(workspaceId))
            return NotFound();

        var companies = await _companyService
            .GetByWorkspaceIdAsync(workspaceId);

        return Ok(companies);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetCompany(
        Guid id,
        [FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(new { message = "WorkspaceId is required." });
        }
        if (!await IsMember(workspaceId))
            return NotFound();

        var company = await _companyService.GetByIdAsync(id, workspaceId);
        return company is null ? NotFound() : Ok(company);
    }

    // POST: api/companies
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompany(
        [FromBody] CreateCompanyRequest request)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "WorkspaceId is required."
            });
        }
        if (!await CanEdit(request.WorkspaceId))
            return NotFound();

        var company = await _companyService.CreateAsync(request);

        return Created($"/api/companies/{company.Id}", company);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> UpdateCompany(
        Guid id,
        [FromQuery] Guid workspaceId,
        [FromBody] UpdateCompanyRequest request)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(new { message = "WorkspaceId is required." });
        }
        if (!await CanEdit(workspaceId))
            return NotFound();

        var company = await _companyService.UpdateAsync(id, workspaceId, request);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id, [FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(new { message = "WorkspaceId is required." });
        }
        if (!await CanEdit(workspaceId))
            return NotFound();

        var deleted = await _companyService.DeleteAsync(id, workspaceId);
        return deleted ? NoContent() : NotFound();
    }

    private Task<bool> IsMember(Guid workspaceId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Task.FromResult(false);
        return WorkspaceAccess.IsMemberAsync(_db, workspaceId, userId);
    }

    private Task<bool> CanEdit(Guid workspaceId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Task.FromResult(false);
        return WorkspaceAccess.CanEditAsync(_db, workspaceId, userId);
    }
}
