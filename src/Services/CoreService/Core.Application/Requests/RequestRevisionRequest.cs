using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record RequestRevisionRequest(CasePhase Phase, string Message, bool InternalOnly);
