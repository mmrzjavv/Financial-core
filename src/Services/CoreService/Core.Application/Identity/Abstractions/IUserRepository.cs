using System.Linq.Expressions;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Identity.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, bool disableTracking = true);
    Task<User?> GetAsync(Expression<Func<User, bool>> predicate, bool disableTracking = true);
    Task<List<User>> GetAllAsync(Expression<Func<User, bool>>? predicate = null, bool disableTracking = true);
    Task<List<User>> GetPagedListAsync(int take, int skip, Expression<Func<User, bool>>? predicate = null, bool disableTracking = true);
    Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null);
    Task<User> AddAsync(User entity);
    Task<User> UpdateAsync(User entity);
    Task DeleteAsync(User entity);
    Task<User?> GetByPhoneAsync(string phoneNumber, bool disableTracking = true);
}
