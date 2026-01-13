using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.ShortLink;

public interface IShortLinkRepository
{
    Task<List<ShortLinkDbTable>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountCreatedByUserInMonthAsync(Guid userId, int year, int month, CancellationToken ct = default);
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct = default);
    Task AddAsync(ShortLinkDbTable entity, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
