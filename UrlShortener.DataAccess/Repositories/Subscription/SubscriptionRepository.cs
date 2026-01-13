using Microsoft.EntityFrameworkCore;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.Subscription;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _db;

    public SubscriptionRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<SubscriptionDbTable?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Subscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Active, ct);

    public Task<List<SubscriptionDbTable>> GetAllAsync(CancellationToken ct = default)
        => _db.Subscriptions
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<SubscriptionDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Subscriptions
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<SubscriptionDbTable>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

    public Task<SubscriptionDbTable?> GetActiveForUserAsync(Guid userId, CancellationToken ct = default)
        => _db.Subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Active)
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => _db.Subscriptions.AnyAsync(x => x.Id == id, ct);

    public async Task AddAsync(SubscriptionDbTable entity, CancellationToken ct = default)
        => await _db.Subscriptions.AddAsync(entity, ct);

    public void Update(SubscriptionDbTable entity)
        => _db.Subscriptions.Update(entity);

    public void Remove(SubscriptionDbTable entity)
        => _db.Subscriptions.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}