using Microsoft.EntityFrameworkCore;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.Plan;


public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _db;

    public PlanRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<PlanDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Plans.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<PlanDbTable>> GetAllAsync(CancellationToken ct = default)
        => _db.Plans.ToListAsync(ct);

    public async Task AddAsync(PlanDbTable plan, CancellationToken ct = default)
        => await _db.Plans.AddAsync(plan, ct);

    public void Update(PlanDbTable plan)
        => _db.Plans.Update(plan);

    public void Remove(PlanDbTable plan)
        => _db.Plans.Remove(plan);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
