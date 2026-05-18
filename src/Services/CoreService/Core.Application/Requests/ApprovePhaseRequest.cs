using Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record ApprovePhaseRequest(CasePhase Phase, string? Comment);