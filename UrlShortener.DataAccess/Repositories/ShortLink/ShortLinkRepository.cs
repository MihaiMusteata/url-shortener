using Microsoft.EntityFrameworkCore;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.ShortLink;

public class ShortLinkRepository : IShortLinkRepository
{
    private readonly AppDbContext _db;

    public ShortLinkRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<ShortLinkDbTable>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.ShortLinks
            .AsNoTracking()
            .Include(x => x.QrCode)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountCreatedByUserInMonthAsync(Guid userId, int year, int month, CancellationToken ct = default)
    {
        return _db.ShortLinks
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.CreatedAt.Year == year && x.CreatedAt.Month == month)
            .CountAsync(ct);
    }
}
