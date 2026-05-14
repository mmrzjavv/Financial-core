using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Services.CoreService.Core.Domain.Entities;

public sealed class InvestmentCaseDataEntry1 : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseDataEntry1()
    {
        StartupTitle = default!;
        BusinessDescription = default!;
    }

    public InvestmentCaseDataEntry1(Guid caseId, string startupTitle, string businessDescription, decimal requestedAmount, int teamSize, string? website)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        StartupTitle = startupTitle;
        BusinessDescription = businessDescription;
        RequestedAmount = requestedAmount;
        TeamSize = teamSize;
        Website = website;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public string StartupTitle { get; private set; }
    public string BusinessDescription { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public int TeamSize { get; private set; }
    public string? Website { get; private set; }

    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? Industry { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string startupTitle, string businessDescription, decimal requestedAmount, int teamSize, string? website, string? country, string? city, string? industry)
    {
        StartupTitle = startupTitle;
        BusinessDescription = businessDescription;
        RequestedAmount = requestedAmount;
        TeamSize = teamSize;
        Website = website;
        Country = country;
        City = city;
        Industry = industry;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

