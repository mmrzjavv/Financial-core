namespace Core.Application.DTOs;

public sealed record UserDisplayDto(
    string UserId,
    string FullName,
    string? PhoneNumber);
