using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Requests;

public sealed record RecordValuationRequest(ValuationType Type, decimal Amount, string? Notes);