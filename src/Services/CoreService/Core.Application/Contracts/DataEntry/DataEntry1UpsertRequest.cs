using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Contracts.DataEntry;

public sealed record DataEntry1UpsertRequest(
    BusinessStage BusinessStage,
    decimal RequestedAmount);
