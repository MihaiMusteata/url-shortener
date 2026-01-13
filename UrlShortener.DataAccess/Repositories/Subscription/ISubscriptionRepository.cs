using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.Subscription;

public interface ISubscriptionRepository
{
    Task<List<SubscriptionDbTable>> GetAllAsync(CancellationToken ct = default);
    Task<SubscriptionDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SubscriptionDbTable>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<SubscriptionDbTable?> GetActiveForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(SubscriptionDbTable entity, CancellationToken ct = default);
    void Update(SubscriptionDbTable entity);
    void Remove(SubscriptionDbTable entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<SubscriptionDbTable?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
}
