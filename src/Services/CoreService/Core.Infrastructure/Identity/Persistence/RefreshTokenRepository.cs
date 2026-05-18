using System.Linq.Expressions;
using Core.Application.Identity.Abstractions;
using Core.Domain.Identity.Entities;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Identity.Persistence;

public sealed class RefreshTokenRepository(CoreDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByIdAsync(Guid id, bool disableTracking = true)
    {
        IQueryable<RefreshToken> query = context.RefreshTokens;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<RefreshToken?> GetAsync(Expression<Func<RefreshToken, bool>> predicate, bool disableTracking = true)
    {
        IQueryable<RefreshToken> query = context.RefreshTokens;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<List<RefreshToken>> GetAllAsync(Expression<Func<RefreshToken, bool>>? predicate = null, bool disableTracking = true)
    {
        IQueryable<RefreshToken> query = context.RefreshTokens;
        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync();
    }

    public async Task<RefreshToken> AddAsync(RefreshToken entity)
    {
        entity.CreateDate = DateTime.UtcNow;
        await context.RefreshTokens.AddAsync(entity);
        return entity;
    }

    public Task<RefreshToken> UpdateAsync(RefreshToken entity)
    {
        entity.UpdateDate = DateTime.UtcNow;
        context.RefreshTokens.Update(entity);
        return Task.FromResult(entity);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, bool disableTracking = true)
        => GetAsync(t => t.TokenHash == tokenHash, disableTracking);

    public async Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, bool disableTracking = true)
    {
        IQueryable<RefreshToken> query = context.RefreshTokens;
        if (disableTracking)
            query = query.AsNoTracking();

        var now = DateTime.UtcNow;
        return await query
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
