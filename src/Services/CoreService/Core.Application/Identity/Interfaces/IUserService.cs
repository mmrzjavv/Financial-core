using BuildingBlocks.Application.Results;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Interfaces;

public interface IUserService
{
    Task<ApiOperationResult<UserDto>> SendOtpAsync(SendOtpDto dto);
  Task<ApiOperationResult<LoginDto>> VerifyOtpAsync(VerifyOtpDto dto);
    Task<ApiOperationResult<LoginDto>> RefreshTokenAsync(RefreshTokenDto dto, string? accessToken);
    Task<ApiOperationResult<UserDto>> LogoutCurrentSessionAsync(Guid userId, Guid sessionId);
    Task<ApiOperationResult<UserDto>> RevokeSessionAsync(Guid userId, Guid sessionId);
    Task<ApiOperationResult<UserDto>> RevokeAllSessionsAsync(Guid userId);
    Task<ApiOperationResult<SessionDto>> GetActiveSessionsAsync(Guid userId);
    Task<ApiOperationResult<UserDto>> GetByIdAsync(Guid id);
    Task<ApiOperationResult<UserDto>> GetPagedAsync(int take, int skip);
    Task<ApiOperationResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ApiOperationResult<UserDto>> UpdateAsync(Guid id, Guid requesterId, UpdateUserDto dto);
    Task<ApiOperationResult<UserDto>> DeleteAsync(Guid id);
    Task<ApiOperationResult<UserDto>> Profile(Guid id);
}
