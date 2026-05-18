using Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record UpdateDataEntry1Request(
    BusinessStage BusinessStage,
    decimal RequestedAmount);
