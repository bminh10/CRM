
using CRM.Api.Business.Interface;
using CRM.Api.Data;
using CRM.Api.DTOs.Companies;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Business.Services;

public class CompanyService : ICompanyService
{
    private readonly CrmDbContext _context;

    public CompanyService(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyResponse>> GetByWorkspaceIdAsync(Guid workspaceId)
    {
        var companies = await _context.Companies
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse
            {
                Id = c.Id,
                WorkspaceId = c.WorkspaceId,
                Name = c.Name,
                TaxCode = c.TaxCode,
                Email = c.Email,
                Phone = c.Phone,
                Website = c.Website,
                Address = c.Address,
                CompanyType = c.CompanyType,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return companies;
    }


    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request)
    {
        var company = new CRM.Api.Models.Company
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Name = request.Name,
            TaxCode = request.TaxCode,
            Email = request.Email,
            Phone = request.Phone,
            Website = request.Website,
            Address = request.Address,
            CompanyType = request.CompanyType,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);

        await _context.SaveChangesAsync();

        var response = new CompanyResponse
        {
            Id = company.Id,
            WorkspaceId = company.WorkspaceId,
            Name = company.Name,
            TaxCode = company.TaxCode,
            Email = company.Email,
            Phone = company.Phone,
            Website = company.Website,
            Address = company.Address,
            CompanyType = company.CompanyType,
            Notes = company.Notes,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };

        return response;
    }

    public async Task<CompanyResponse?> GetByIdAsync(Guid companyId, Guid workspaceId)
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId && c.WorkspaceId == workspaceId)
            .Select(c => new CompanyResponse
            {
                Id = c.Id,
                WorkspaceId = c.WorkspaceId,
                Name = c.Name,
                TaxCode = c.TaxCode,
                Email = c.Email,
                Phone = c.Phone,
                Website = c.Website,
                Address = c.Address,
                CompanyType = c.CompanyType,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<CompanyResponse?> UpdateAsync(
        Guid companyId,
        Guid workspaceId,
        UpdateCompanyRequest request)
    {
        var company = await _context.Companies
            .SingleOrDefaultAsync(c => c.Id == companyId && c.WorkspaceId == workspaceId);

        if (company is null)
        {
            return null;
        }

        company.Name = request.Name;
        company.TaxCode = request.TaxCode;
        company.Email = request.Email;
        company.Phone = request.Phone;
        company.Website = request.Website;
        company.Address = request.Address;
        company.CompanyType = request.CompanyType;
        company.Notes = request.Notes;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(companyId, workspaceId);
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid workspaceId)
    {
        var company = await _context.Companies
            .SingleOrDefaultAsync(c => c.Id == companyId && c.WorkspaceId == workspaceId);

        if (company is null)
        {
            return false;
        }

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return true;
    }
}
