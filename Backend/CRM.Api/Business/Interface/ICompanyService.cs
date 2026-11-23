using CRM.Api.DTOs.Companies;

namespace CRM.Api.Business.Interface;

public interface ICompanyService
{
    Task<List<CompanyResponse>> GetByWorkspaceIdAsync(Guid workspaceId);
    Task<CompanyResponse?> GetByIdAsync(Guid companyId, Guid workspaceId);
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request);
    Task<CompanyResponse?> UpdateAsync(Guid companyId, Guid workspaceId, UpdateCompanyRequest request);
    Task<bool> DeleteAsync(Guid companyId, Guid workspaceId);
}
