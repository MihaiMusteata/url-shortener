using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.ShortLink;

public interface IShortLinkRepository
{
    Task<List<ShortLinkDbTable>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountCreatedByUserInMonthAsync(Guid userId, int year, int month, CancellationToken ct = default);
}
