using Core.Application.Identity.Common.DTOs;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Interfaces;

public interface IUserService
{
    Task<PanelOperationResult<UserDto>> SendOtpAsync(SendOtpDto dto);
  Task<PanelOperationResult<LoginDto>> VerifyOtpAsync(VerifyOtpDto dto);
    Task<PanelOperationResult<LoginDto>> RefreshTokenAsync(RefreshTokenDto dto, string? accessToken);
    Task<PanelOperationResult<UserDto>> LogoutCurrentSessionAsync(Guid userId, Guid sessionId);
    Task<PanelOperationResult<UserDto>> RevokeSessionAsync(Guid userId, Guid sessionId);
    Task<PanelOperationResult<UserDto>> RevokeAllSessionsAsync(Guid userId);
    Task<PanelOperationResult<SessionDto>> GetActiveSessionsAsync(Guid userId);
    Task<PanelOperationResult<UserDto>> GetByIdAsync(Guid id);
    Task<PanelOperationResult<UserDto>> GetPagedAsync(int take, int skip);
    Task<PanelOperationResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<PanelOperationResult<UserDto>> UpdateAsync(Guid id, Guid requesterId, UpdateUserDto dto);
    Task<PanelOperationResult<UserDto>> DeleteAsync(Guid id);
    Task<PanelOperationResult<UserDto>> Profile(Guid id);
}
