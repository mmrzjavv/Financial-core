namespace BuildingBlocks.Domain.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, int statusCode = 400, string? errorCode = "business_rule_violation")
        : base(message, statusCode, errorCode)
    {
    }
}
