using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Contracts.Reviews;

public sealed record RequestRevisionForPhaseRequest(CasePhase Phase, string Message, bool IsInternal);

