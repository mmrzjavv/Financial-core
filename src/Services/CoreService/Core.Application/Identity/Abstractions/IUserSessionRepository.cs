using System.Linq.Expressions;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Identity.Abstractions;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, bool disableTracking = true);
    Task<UserSession?> GetAsync(Expression<Func<UserSession, bool>> predicate, bool disableTracking = true);
    Task<List<UserSession>> GetAllAsync(Expression<Func<UserSession, bool>>? predicate = null, bool disableTracking = true);
    Task<UserSession> AddAsync(UserSession entity);
    Task<UserSession> UpdateAsync(UserSession entity);
    Task<UserSession?> GetBySessionIdAsync(Guid sessionId, bool disableTracking = true);
    Task<List<UserSession>> GetActiveByUserAsync(Guid userId, bool disableTracking = true);
}
