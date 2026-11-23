
using System.ComponentModel.DataAnnotations;

namespace CRM.Api.DTOs.Companies;

public class CreateCompanyRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Address { get; set; }

    [Required]
    [RegularExpression("^(customer|partner|both)$",
        ErrorMessage = "Company type must be customer, partner or both.")]
    public string CompanyType { get; set; } = "customer";

    public string? Notes { get; set; }

    [Required]
    public Guid WorkspaceId { get; set; }
}
