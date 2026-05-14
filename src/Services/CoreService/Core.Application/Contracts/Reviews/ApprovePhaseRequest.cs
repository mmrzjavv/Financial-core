using Services.CoreService.Core.Domain.Enums;


namespace Services.CoreService.Core.Application.Contracts.Reviews;

public sealed record ApprovePhaseRequest(CasePhase Phase, string? Comment);
