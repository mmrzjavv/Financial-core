using System.Linq.Expressions;
using Core.Application.Identity.Abstractions;
using Services.CoreService.Core.Domain.Identity.Entities;
using Services.CoreService.Core.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Identity.Persistence;

public sealed class UserRepository(PanelContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, bool disableTracking = true)
    {
        IQueryable<User> query = context.Users;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<User?> GetAsync(Expression<Func<User, bool>> predicate, bool disableTracking = true)
    {
        IQueryable<User> query = context.Users;
        if (disableTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<List<User>> GetAllAsync(Expression<Func<User, bool>>? predicate = null, bool disableTracking = true)
    {
        IQueryable<User> query = context.Users;
        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync();
    }

    public async Task<List<User>> GetPagedListAsync(int take, int skip, Expression<Func<User, bool>>? predicate = null, bool disableTracking = true)
    {
        IQueryable<User> query = context.Users;
        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null)
    {
        IQueryable<User> query = context.Users;
        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<User> AddAsync(User entity)
    {
        entity.CreateDate = DateTime.UtcNow;
        await context.Users.AddAsync(entity);
        return entity;
    }

    public Task<User> UpdateAsync(User entity)
    {
        entity.UpdateDate = DateTime.UtcNow;
        context.Users.Update(entity);
        return Task.FromResult(entity);
    }

    public async Task DeleteAsync(User entity)
    {
        entity.IsDeleted = true;
        entity.UpdateDate = DateTime.UtcNow;
        context.Users.Update(entity);
        await Task.CompletedTask;
    }

    public Task<User?> GetByPhoneAsync(string phoneNumber, bool disableTracking = true)
        => GetAsync(u => u.PhoneNumber == phoneNumber, disableTracking);
}
