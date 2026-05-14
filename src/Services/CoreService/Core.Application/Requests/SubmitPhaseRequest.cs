using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record SubmitPhaseRequest(CasePhase Phase);
