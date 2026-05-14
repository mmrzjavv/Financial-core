namespace Core.Application.Identity.Abstractions;

public sealed class AccessTokenPayload
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime Expiration { get; set; }
}
