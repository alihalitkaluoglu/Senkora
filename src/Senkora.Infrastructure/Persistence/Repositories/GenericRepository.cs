using Microsoft.EntityFrameworkCore;
using Senkora.Domain.Interfaces.Repositories;

namespace Senkora.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(ApplicationDbContext db) : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Db    = db;
    protected readonly DbSet<T>            DbSet = db.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await Db.SaveChangesAsync(ct);
}
