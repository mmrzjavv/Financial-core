using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record DataEntry1Dto(
    string RepresentativeFullName,
    BusinessStage BusinessStage,
    string ContactEmail,
    decimal RequestedAmount);
