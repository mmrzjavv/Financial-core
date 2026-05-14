using System.Linq.Expressions;
using Core.Application.Identity.Abstractions;
using Services.CoreService.Core.Domain.Identity.Entities;
using Services.CoreService.Core.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Identity.Persistence;

public sealed class UserSessionRepository(PanelContext context) : IUserSessionRepository
{
    public async Task<UserSession?> GetByIdAsync(Guid id, bool disableTracking = true)
    {
        IQueryable<UserSession> query = context.UserSessions;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<UserSession?> GetAsync(Expression<Func<UserSession, bool>> predicate, bool disableTracking = true)
    {
        IQueryable<UserSession> query = context.UserSessions;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<List<UserSession>> GetAllAsync(Expression<Func<UserSession, bool>>? predicate = null, bool disableTracking = true)
    {
        IQueryable<UserSession> query = context.UserSessions;
        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync();
    }

    public async Task<UserSession> AddAsync(UserSession entity)
    {
        entity.CreateDate = DateTime.UtcNow;
        await context.UserSessions.AddAsync(entity);
        return entity;
    }

    public Task<UserSession> UpdateAsync(UserSession entity)
    {
        entity.UpdateDate = DateTime.UtcNow;
        context.UserSessions.Update(entity);
        return Task.FromResult(entity);
    }

    public Task<UserSession?> GetBySessionIdAsync(Guid sessionId, bool disableTracking = true)
        => GetAsync(s => s.SessionId == sessionId, disableTracking);

    public async Task<List<UserSession>> GetActiveByUserAsync(Guid userId, bool disableTracking = true)
    {
        IQueryable<UserSession> query = context.UserSessions;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }
}
