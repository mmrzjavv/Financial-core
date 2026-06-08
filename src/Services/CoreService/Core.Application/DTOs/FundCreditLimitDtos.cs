using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record FundCreditCapacitySnapshotDto(
    FundModuleType ModuleType,
    decimal? TotalPeriodAllocation,
    decimal? TotalUtilized,
    decimal? RemainingCapacity,
    DateOnly? PeriodStart,
    DateOnly? ExpiresAt);

public sealed record FundCreditLimitDto(
    Guid Id,
    FundModuleType ModuleType,
    decimal CreditLimitWithCheck,
    DateOnly PeriodStart,
    DateOnly ExpiresAt,
    decimal TotalUtilized,
    decimal RemainingCapacity,
    string? LastSetByUserId,
    string? LastSetByFullName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record FundCreditLimitDashboardSectionDto(
    IReadOnlyList<FundCreditLimitDto> ActivePools,
    IReadOnlyList<FundCreditLimitDto> HistoricalPools);
