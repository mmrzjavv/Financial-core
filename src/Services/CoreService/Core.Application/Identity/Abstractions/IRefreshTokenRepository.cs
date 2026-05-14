using System.Linq.Expressions;
using Services.CoreService.Core.Domain.Identity.Entities;


namespace Core.Application.Identity.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByIdAsync(Guid id, bool disableTracking = true);
    Task<RefreshToken?> GetAsync(Expression<Func<RefreshToken, bool>> predicate, bool disableTracking = true);
    Task<List<RefreshToken>> GetAllAsync(Expression<Func<RefreshToken, bool>>? predicate = null, bool disableTracking = true);
    Task<RefreshToken> AddAsync(RefreshToken entity);
    Task<RefreshToken> UpdateAsync(RefreshToken entity);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, bool disableTracking = true);
    Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, bool disableTracking = true);
}