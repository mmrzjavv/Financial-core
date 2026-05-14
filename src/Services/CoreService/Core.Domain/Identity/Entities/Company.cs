using System.ComponentModel.DataAnnotations.Schema;

namespace Services.CoreService.Core.Domain.Identity.Entities;

[Table("Company", Schema = "Identity")]
public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    public List<User> Users { get; set; } = [];
}

