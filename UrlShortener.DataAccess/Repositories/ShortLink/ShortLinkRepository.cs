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
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.CreatedAt.Year == year && x.CreatedAt.Month == month)
            .CountAsync(ct);
    }

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct = default)
        => _db.ShortLinks.AsNoTracking().AnyAsync(x => x.ShortCode == shortCode, ct);

    public Task AddAsync(ShortLinkDbTable entity, CancellationToken ct = default)
        => _db.ShortLinks.AddAsync(entity, ct).AsTask();

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<ShortLinkDbTable?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => _db.ShortLinks
            .Include(x => x.QrCode)
            .Include(x => x.LinkClicks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ShortLinkDbTable?> GetByShortCodeAsync(string shortCode, CancellationToken ct)
        => _db.ShortLinks.FirstOrDefaultAsync(x => x.ShortCode == shortCode, ct);
    
    
    public void SoftDelete(ShortLinkDbTable entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.IsActive = false;
        _db.ShortLinks.Update(entity);
    }

    public Task<ShortLinkDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ShortLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
}
